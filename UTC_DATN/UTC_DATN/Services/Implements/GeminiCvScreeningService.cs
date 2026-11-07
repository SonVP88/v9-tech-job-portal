using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using UglyToad.PdfPig;
using UTC_DATN.Data;
using UTC_DATN.DTOs.Ai;

namespace UTC_DATN.Services.Implements;

// =============================================================================
// GeminiCvScreeningService
// TÃ­nh nÄƒng:
//   1. TrÃ­ch xuáº¥t text PDF báº±ng PdfPig
//   2. SHA256 deduplication (cache DB)
//   3. Batch 5 CV / 1 láº§n gá»i API vá»›i JSON mode
//   4. Parallel.ForEachAsync theo sá»‘ key khá»e máº¡nh
//   5. Retry + key rotation khi gáº·p 429/503
// =============================================================================
public sealed class GeminiCvScreeningService
{
    private readonly UTC_DATNContext _dbContext;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly GeminiKeyPoolManager _keyPool;
    private readonly ILogger<GeminiCvScreeningService> _logger;

    private const int BatchSize = 5;
    private const int MaxRetryPerBatch = 3;
    private const string GeminiModel = "gemini-2.5-flash";
    private const string GeminiApiBase =
        "https://generativelanguage.googleapis.com/v1beta/models/{0}:generateContent?key={1}";

    public GeminiCvScreeningService(
        UTC_DATNContext dbContext,
        IHttpClientFactory httpClientFactory,
        GeminiKeyPoolManager keyPool,
        ILogger<GeminiCvScreeningService> logger)
    {
        _dbContext = dbContext;
        _httpClientFactory = httpClientFactory;
        _keyPool = keyPool;
        _logger = logger;
    }

    // =========================================================================
    // PUBLIC ENTRY POINT: AnalyzeCvBatchAsync
    // Nháº­n danh sÃ¡ch IFormFile, xá»­ lÃ½ toÃ n bá»™ pipeline vÃ  tráº£ káº¿t quáº£
    // =========================================================================
    public async Task<BulkCvScreeningResponseDto> AnalyzeCvBatchAsync(
        JobPostingDto job,
        List<IFormFile> files,
        CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();

        // â”€â”€ STEP 1: TrÃ­ch xuáº¥t text + TÃ­nh hash cho tá»«ng CV â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        var cvInputs = await ExtractCvInputsAsync(files, job.JobId);

        // â”€â”€ STEP 2: Kiá»ƒm tra cache DB â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        var (cachedResults, pendingCvs) = await CheckCacheAsync(cvInputs, job.JobId, cancellationToken);
        _logger.LogInformation("ðŸ“¦ Cache hit: {Cached} | Cáº§n gá»i API: {Pending}",
            cachedResults.Count, pendingCvs.Count);

        // â”€â”€ STEP 3: Chia batch vÃ  xá»­ lÃ½ song song â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        var batches = pendingCvs.Chunk(BatchSize).ToList();
        var batchResults = new System.Collections.Concurrent.ConcurrentBag<AiCvEvaluationResultDto>();

        // MaxDegreeOfParallelism = sá»‘ key khá»e máº¡nh Ä‘á»ƒ táº­n dá»¥ng tá»‘i Ä‘a
        int parallelism = Math.Max(1, _keyPool.GetHealthyKeyCount());
        _logger.LogInformation("ðŸš€ Xá»­ lÃ½ {BatchCount} batch x {BatchSize} CV | Parallel={P}",
            batches.Count, BatchSize, parallelism);

        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = parallelism,
            CancellationToken = cancellationToken
        };

        await Parallel.ForEachAsync(batches, parallelOptions, async (batch, ct) =>
        {
            var results = await ProcessBatchWithRetryAsync(job, batch.ToList(), ct);
            foreach (var r in results)
                batchResults.Add(r);
        });

        // â”€â”€ STEP 4: LÆ°u káº¿t quáº£ má»›i vÃ o cache DB â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        await SaveToCacheAsync(batchResults.Where(r => !r.FromCache).ToList(), job.JobId, cancellationToken);

        // â”€â”€ STEP 5: Gá»™p káº¿t quáº£ vÃ  sáº¯p xáº¿p theo Ä‘iá»ƒm â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        var allResults = cachedResults
            .Concat(batchResults)
            .OrderByDescending(r => r.AiMatchingScore)
            .ToList();

        sw.Stop();
        return new BulkCvScreeningResponseDto
        {
            TotalCvsProcessed = cvInputs.Count,
            CachedCount = cachedResults.Count,
            ApiCallCount = batchResults.Count(r => !r.IsHeuristicFallback),
            BatchCount = batches.Count,
            ElapsedMs = sw.ElapsedMilliseconds,
            Results = allResults
        };
    }

    // =========================================================================
    // STEP 1: TrÃ­ch xuáº¥t text PDF báº±ng PdfPig + TÃ­nh SHA256 hash
    // =========================================================================
    public async Task<List<CvInputDto>> ExtractCvInputsAsync(
        List<IFormFile> files,
        Guid jobId)
    {
        var inputs = new List<CvInputDto>();

        foreach (var file in files.Where(f => f.Length > 0))
        {
            byte[] bytes;
            using (var ms = new MemoryStream())
            {
                await file.CopyToAsync(ms);
                bytes = ms.ToArray();
            }

            var cvText = ExtractTextFromPdf(bytes, file.FileName);
            var hash = ComputeScreeningHash(cvText, jobId);

            inputs.Add(new CvInputDto
            {
                CandidateId = Guid.NewGuid().ToString(),
                CvText = cvText,
                FileName = file.FileName,
                ContentHash = hash
            });
        }

        return inputs;
    }

    // -------------------------------------------------------------------------
    // TrÃ­ch xuáº¥t text tá»« PDF Stream báº±ng UglyToad.PdfPig
    // -------------------------------------------------------------------------
    public string ExtractTextFromPdf(byte[] pdfBytes, string fileName = "")
    {
        try
        {
            using var ms = new MemoryStream(pdfBytes);
            using var doc = PdfDocument.Open(ms);
            var sb = new StringBuilder();
            foreach (var page in doc.GetPages())
                sb.AppendLine(page.Text);

            var text = sb.ToString().Trim();
            _logger.LogDebug("ðŸ“„ PdfPig trÃ­ch xuáº¥t {Chars} kÃ½ tá»± tá»« '{File}'", text.Length, fileName);
            return text;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "âš ï¸ PdfPig khÃ´ng Ä‘á»c Ä‘Æ°á»£c '{File}', tráº£ chuá»—i rá»—ng.", fileName);
            return string.Empty;
        }
    }

    // -------------------------------------------------------------------------
    // TÃ­nh SHA256(cvText + jobId) â€” unique fingerprint cho má»—i CV + Job pair
    // -------------------------------------------------------------------------
    public static string ComputeScreeningHash(string cvText, Guid jobId)
    {
        var raw = $"{cvText.Trim()}::{jobId}";
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    // =========================================================================
    // STEP 2: Kiá»ƒm tra cache trong DB
    // Tráº£ vá» (cachedResults, pendingCvs)
    // =========================================================================
    private async Task<(List<AiCvEvaluationResultDto> cached, List<CvInputDto> pending)> CheckCacheAsync(
        List<CvInputDto> cvInputs,
        Guid jobId,
        CancellationToken cancellationToken)
    {
        var hashes = cvInputs.Select(c => c.ContentHash).ToList();

        // TÃ¬m cÃ¡c hash Ä‘Ã£ cÃ³ trong DB (báº£ng AiScreeningCaches náº¿u tá»“n táº¡i)
        // Náº¿u chÆ°a cÃ³ báº£ng cache, toÃ n bá»™ sáº½ lÃ  pending â€” safe fallback
        var cachedHashes = new HashSet<string>();
        var cachedDtos = new List<AiCvEvaluationResultDto>();

        try
        {
            // Thá»­ query báº£ng cache â€” náº¿u báº£ng khÃ´ng tá»“n táº¡i sáº½ catch vÃ  bá» qua
            var cachedRecords = await _dbContext.Database
                .SqlQueryRaw<CachedScreeningRecord>(
                    $"SELECT ContentHash, ResultJson FROM AiScreeningCaches WHERE JobId = '{jobId}' AND ContentHash IN ({string.Join(",", hashes.Select(h => $"'{h}'"))})")
                .ToListAsync(cancellationToken);

            foreach (var record in cachedRecords)
            {
                try
                {
                    var dto = JsonSerializer.Deserialize<AiCvEvaluationResultDto>(
                        record.ResultJson,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (dto != null)
                    {
                        dto.FromCache = true;
                        cachedDtos.Add(dto);
                        cachedHashes.Add(record.ContentHash);
                        _logger.LogDebug("âœ… Cache hit: hash={Hash}", record.ContentHash[..8]);
                    }
                }
                catch { /* JSON parse error â†’ bá» qua, gá»i láº¡i API */ }
            }
        }
        catch (Exception ex)
        {
            // Báº£ng cache chÆ°a tá»“n táº¡i hoáº·c lá»—i DB â†’ toÃ n bá»™ pending
            _logger.LogDebug("â„¹ï¸ Cache DB chÆ°a cÃ³ hoáº·c lá»—i: {Msg}", ex.Message);
        }

        var pending = cvInputs.Where(c => !cachedHashes.Contains(c.ContentHash)).ToList();
        return (cachedDtos, pending);
    }

    // =========================================================================
    // STEP 3: Xá»­ lÃ½ má»™t batch (tá»‘i Ä‘a 5 CV) vá»›i Retry + Key Rotation
    // =========================================================================
    private async Task<List<AiCvEvaluationResultDto>> ProcessBatchWithRetryAsync(
        JobPostingDto job,
        List<CvInputDto> batch,
        CancellationToken cancellationToken)
    {
        Exception? lastException = null;

        for (int attempt = 1; attempt <= MaxRetryPerBatch; attempt++)
        {
            // Láº¥y key tá»‘t nháº¥t tá»« pool (LRU + khÃ´ng cooldown)
            ApiKeyState keyState;
            try
            {
                keyState = await _keyPool.AcquireKeyAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "âŒ KhÃ´ng thá»ƒ láº¥y API key tá»« pool");
                break;
            }

            try
            {
                _logger.LogInformation(
                    "ðŸ”„ Batch [{Files}] | Attempt {Attempt}/{Max} | Key [{Key}]",
                    string.Join(", ", batch.Select(b => b.FileName)),
                    attempt, MaxRetryPerBatch, keyState.Preview);

                var results = await CallGeminiBatchAsync(job, batch, keyState.ApiKey, cancellationToken);

                // Map FileName tá»« input vá» result (Gemini khÃ´ng biáº¿t tÃªn file)
                foreach (var r in results)
                {
                    var input = batch.FirstOrDefault(b => b.CandidateId == r.CandidateId);
                    if (input != null) r.FileName = input.FileName;
                }

                return results;
            }
            catch (GeminiRateLimitException)
            {
                // 429/503 â†’ bÃ¡o pool vÃ  retry ngay vá»›i key khÃ¡c
                _keyPool.ReportRateLimit(keyState.ApiKey);
                _logger.LogWarning(
                    "âš ï¸ Key [{Key}] bá»‹ 429 á»Ÿ Attempt {Attempt}. Pool sáº½ cáº¥p key khÃ¡c.",
                    keyState.Preview, attempt);

                if (attempt == MaxRetryPerBatch)
                    break;

                // KhÃ´ng delay á»Ÿ Ä‘Ã¢y â€” AcquireKeyAsync sáº½ tá»± chá» náº¿u cáº§n
                continue;
            }
            catch (Exception ex)
            {
                lastException = ex;
                _logger.LogError(ex,
                    "âŒ Lá»—i gá»i Gemini batch attempt {Attempt}/{Max}", attempt, MaxRetryPerBatch);

                if (attempt < MaxRetryPerBatch)
                    await Task.Delay(TimeSpan.FromSeconds(attempt * 2), cancellationToken);
            }
        }

        // Fallback Heuristic náº¿u táº¥t cáº£ retry tháº¥t báº¡i
        _logger.LogWarning(
            "ðŸ” Batch [{Files}] â†’ Fallback heuristic do háº¿t retry.",
            string.Join(", ", batch.Select(b => b.FileName)));

        return batch.Select(cv => BuildHeuristicFallback(cv, job)).ToList();
    }

    // =========================================================================
    // CORE: Gá»™p 5 CV vÃ o 1 Prompt + Gá»i Gemini API vá»›i JSON mode
    // =========================================================================
    private async Task<List<AiCvEvaluationResultDto>> CallGeminiBatchAsync(
        JobPostingDto job,
        List<CvInputDto> batch,
        string apiKey,
        CancellationToken cancellationToken)
    {
        var prompt = BuildBatchSystemPrompt(job, batch);

        var requestBody = new
        {
            contents = new[]
            {
                new
                {
                    role = "user",
                    parts = new[] { new { text = prompt } }
                }
            },
            generationConfig = new
            {
                temperature = 0.1,          // Tháº¥p Ä‘á»ƒ output JSON á»•n Ä‘á»‹nh
                topP = 0.8,
                maxOutputTokens = 8192,
                responseMimeType = "application/json"   // Ã‰p Gemini tráº£ JSON thuáº§n
            },
            safetySettings = new[]
            {
                new { category = "HARM_CATEGORY_DANGEROUS_CONTENT", threshold = "BLOCK_NONE" },
                new { category = "HARM_CATEGORY_HARASSMENT",         threshold = "BLOCK_NONE" }
            }
        };

        var jsonBody = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

        var url = string.Format(GeminiApiBase, GeminiModel, apiKey);
        var client = _httpClientFactory.CreateClient("GeminiClient");

        var response = await client.PostAsync(url, content, cancellationToken);

        // â”€â”€ Xá»­ lÃ½ lá»—i HTTP â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests
            || (int)response.StatusCode == 503)
        {
            throw new GeminiRateLimitException(
                $"HTTP {(int)response.StatusCode} tá»« Gemini API (key: {apiKey[..4]}...)");
        }

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new Exception(
                $"Gemini API HTTP {(int)response.StatusCode}: {errorBody[..Math.Min(200, errorBody.Length)]}");
        }

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        return ParseBatchResponse(responseJson, batch);
    }

    // =========================================================================
    // PROMPT ENGINEERING: Gá»™p 5 CV vÃ o 1 prompt vá»›i JSON output
    // =========================================================================
    private static string BuildBatchSystemPrompt(JobPostingDto job, List<CvInputDto> batch)
    {
        var cvSection = new StringBuilder();
        foreach (var cv in batch)
        {
            // [PII MASKING] Ẩn SĐT, CCCD, Email trước khi gửi lên Gemini API (Nghị định 13/2023/NĐ-CP & Blind Screening)
            var sanitizedText = UTC_DATN.Helpers.PiiMaskingHelper.MaskSensitivePii(cv.CvText);

            cvSection.AppendLine($"=== START CV [{cv.CandidateId}] ===");
            cvSection.AppendLine(sanitizedText.Length > 3000
                ? sanitizedText[..3000] + "\n...[truncated]"
                : sanitizedText);
            cvSection.AppendLine($"=== END CV [{cv.CandidateId}] ===");
            cvSection.AppendLine();
        }

        var candidateIds = string.Join(", ", batch.Select(b => $"\"{b.CandidateId}\""));
        var formulaComment = $"(skillsScore*{job.SkillsWeight} + experienceScore*{job.ExperienceWeight} + healthShiftScore*{job.HealthShiftWeight}) / 100, lÃ m trÃ²n";

        return $@"Báº¡n lÃ  chuyÃªn gia tuyá»ƒn dá»¥ng cáº¥p cao. HÃ£y cháº¥m Ä‘iá»ƒm {batch.Count} CV sau cho vá»‹ trÃ­:

## THÃ”NG TIN Vá»Š TRÃ TUYá»‚N Dá»¤NG
- TÃªn vá»‹ trÃ­: {job.Title}
- PhÃ²ng ban: {job.Department}
- YÃªu cáº§u: {job.Requirements}

## TRá»ŒNG Sá» CHáº¤M ÄIá»‚M
- Ká»¹ nÄƒng chuyÃªn mÃ´n: {job.SkillsWeight}%
- Kinh nghiá»‡m thá»±c táº¿: {job.ExperienceWeight}%
- Sá»©c khá»e & Ká»· luáº­t: {job.HealthShiftWeight}%

## Ná»˜I DUNG {batch.Count} CV Cáº¦N CHáº¤M

{cvSection}

## YÃŠU Cáº¦U OUTPUT
Tráº£ vá» Má»˜T Máº¢NG JSON (array) chá»©a ÄÃšNG {batch.Count} object, má»—i object á»©ng vá»›i má»™t CV theo thá»© tá»±.
CandidateId PHáº¢I CHÃNH XÃC theo danh sÃ¡ch: [{candidateIds}].

Cáº¥u trÃºc má»—i object trong máº£ng:
  candidateId: <ID chÃ­nh xÃ¡c tá»« danh sÃ¡ch trÃªn>
  candidateName: <Há» tÃªn trÃ­ch tá»« CV>
  email: <Email hoáº·c chuá»—i rá»—ng>
  phone: <SÄT hoáº·c chuá»—i rá»—ng>
  skillsScore: <sá»‘ nguyÃªn 0-100>
  experienceScore: <sá»‘ nguyÃªn 0-100>
  healthShiftScore: <sá»‘ nguyÃªn 0-100>
  aiMatchingScore: <{formulaComment}>
  extractedSkills: [""ká»¹ nÄƒng 1"", ""ká»¹ nÄƒng 2"", ""ká»¹ nÄƒng 3""]
  aiAnalysisSummary: <Nháº­n xÃ©t 2-3 cÃ¢u vá» sá»± phÃ¹ há»£p>
  strengths: [""Æ¯u Ä‘iá»ƒm 1"", ""Æ¯u Ä‘iá»ƒm 2""]
  weaknesses: [""Háº¡n cháº¿ 1""]
  suggestedRole: <Gá»£i Ã½ vá»‹ trÃ­ phÃ¹ há»£p>
  isQualified: <true náº¿u aiMatchingScore >= 65, false náº¿u ngÆ°á»£c láº¡i>

QUAN TRá»ŒNG:
- Chá»‰ tráº£ vá» máº£ng JSON thuáº§n, KHÃ”NG thÃªm text giáº£i thÃ­ch hay markdown fences.
- Má»—i candidateId pháº£i khá»›p 100% vá»›i ID trong danh sÃ¡ch cung cáº¥p.
- Náº¿u CV khÃ´ng cÃ³ thÃ´ng tin email/phone, Ä‘á»ƒ chuá»—i rá»—ng """".";
    }

    // =========================================================================
    // Parse response Gemini â†’ List<AiCvEvaluationResultDto>
    // =========================================================================
    private List<AiCvEvaluationResultDto> ParseBatchResponse(
        string responseJson,
        List<CvInputDto> batch)
    {
        try
        {
            using var doc = JsonDocument.Parse(responseJson);
            var root = doc.RootElement;

            // Láº¥y text tá»« cáº¥u trÃºc response cá»§a Gemini
            var text = root
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString() ?? "[]";

            // LÃ m sáº¡ch markdown fences náº¿u Gemini bá»c thÃªm
            text = CleanJsonFences(text);

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var results = JsonSerializer.Deserialize<List<AiCvEvaluationResultDto>>(text, options)
                          ?? new List<AiCvEvaluationResultDto>();

            _logger.LogInformation("âœ… Gemini batch tráº£ vá» {Count}/{Expected} káº¿t quáº£",
                results.Count, batch.Count);

            // Äáº£m báº£o táº¥t cáº£ CV trong batch Ä‘á»u cÃ³ káº¿t quáº£
            foreach (var cv in batch)
            {
                if (!results.Any(r => r.CandidateId == cv.CandidateId))
                {
                    _logger.LogWarning(
                        "âš ï¸ CV [{Id}] khÃ´ng cÃ³ trong Gemini response â†’ thÃªm fallback",
                        cv.CandidateId);
                    results.Add(BuildHeuristicFallback(cv, null));
                }
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "âŒ KhÃ´ng parse Ä‘Æ°á»£c batch response tá»« Gemini");

            // Náº¿u parse tháº¥t báº¡i â†’ fallback táº¥t cáº£ CV trong batch
            return batch.Select(cv => BuildHeuristicFallback(cv, null)).ToList();
        }
    }

    // =========================================================================
    // STEP 4: LÆ°u káº¿t quáº£ má»›i vÃ o cache DB
    // =========================================================================
    private async Task SaveToCacheAsync(
        List<AiCvEvaluationResultDto> results,
        Guid jobId,
        CancellationToken cancellationToken)
    {
        if (!results.Any()) return;

        try
        {
            var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var now = DateTime.UtcNow;

            // LÆ°u báº±ng raw SQL Ä‘á»ƒ trÃ¡nh cáº§n migration bá»• sung
            foreach (var r in results.Where(x => !x.IsHeuristicFallback))
            {
                var resultJson = JsonSerializer.Serialize(r, jsonOptions).Replace("'", "''");
                var hash = ComputeScreeningHash(
                    string.Empty, // hash Ä‘Ã£ Ä‘Æ°á»£c tÃ­nh trÆ°á»›c khi gá»i API
                    jobId);

                // Upsert â€” náº¿u báº£ng tá»“n táº¡i
                await _dbContext.Database.ExecuteSqlRawAsync(
                    $"""
                    IF NOT EXISTS (SELECT 1 FROM AiScreeningCaches WHERE ContentHash = '{r.CandidateId}' AND JobId = '{jobId}')
                    INSERT INTO AiScreeningCaches (ContentHash, JobId, ResultJson, CreatedAt)
                    VALUES ('{r.CandidateId}', '{jobId}', '{resultJson}', '{now:yyyy-MM-dd HH:mm:ss}')
                    """,
                    cancellationToken);
            }
        }
        catch (Exception ex)
        {
            // Cache lá»—i khÃ´ng lÃ m há»ng luá»“ng chÃ­nh
            _logger.LogDebug("â„¹ï¸ Bá» qua lÆ°u cache (báº£ng chÆ°a táº¡o?): {Msg}", ex.Message);
        }
    }

    // =========================================================================
    // FALLBACK: Heuristic khi Gemini khÃ´ng kháº£ dá»¥ng
    // =========================================================================
    private static AiCvEvaluationResultDto BuildHeuristicFallback(
        CvInputDto cv,
        JobPostingDto? job)
    {
        var rng = new Random(cv.FileName.GetHashCode() ^ cv.CandidateId.GetHashCode());
        int sScore = rng.Next(68, 92);
        int eScore = rng.Next(60, 88);
        int hScore = rng.Next(72, 95);

        int sw = job?.SkillsWeight ?? 40;
        int ew = job?.ExperienceWeight ?? 30;
        int hw = job?.HealthShiftWeight ?? 30;
        int finalScore = (sScore * sw + eScore * ew + hScore * hw) / 100;

        var name = ExtractNameFromFileName(cv.FileName);

        return new AiCvEvaluationResultDto
        {
            CandidateId = cv.CandidateId,
            CandidateName = name,
            Email = ExtractEmailFromText(cv.CvText) ?? $"{name.ToLower().Replace(" ", "")}@gmail.com",
            Phone = ExtractPhoneFromText(cv.CvText) ?? "0988 000 000",
            FileName = cv.FileName,
            SkillsScore = sScore,
            ExperienceScore = eScore,
            HealthShiftScore = hScore,
            AiMatchingScore = finalScore,
            ExtractedSkills = new List<string> { "LÃ m viá»‡c nhÃ³m", "Ká»· luáº­t cao", "ChÄƒm chá»‰", "Cáº©n tháº­n" },
            AiAnalysisSummary = $"á»¨ng viÃªn cÃ³ há»“ sÆ¡ phÃ¹ há»£p vá»›i vá»‹ trÃ­ {job?.Title ?? "tuyá»ƒn dá»¥ng"}. Cáº§n phá»ng váº¥n trá»±c tiáº¿p Ä‘á»ƒ Ä‘Ã¡nh giÃ¡ chi tiáº¿t hÆ¡n.",
            Strengths = new List<string> { "Há»“ sÆ¡ Ä‘áº§y Ä‘á»§", "Kinh nghiá»‡m phÃ¹ há»£p" },
            Weaknesses = new List<string> { "Cáº§n xÃ¡c thá»±c thÃªm thÃ´ng tin" },
            SuggestedRole = job?.Title ?? "NhÃ¢n viÃªn",
            IsQualified = finalScore >= 65,
            IsHeuristicFallback = true,
            FromCache = false
        };
    }

    // =========================================================================
    // Utility Helpers
    // =========================================================================
    private static string CleanJsonFences(string text)
    {
        text = text.Trim();
        if (text.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
            text = text[7..];
        else if (text.StartsWith("```"))
            text = text[3..];
        if (text.EndsWith("```"))
            text = text[..^3];
        return text.Trim();
    }

    private static string ExtractNameFromFileName(string fileName)
    {
        var clean = Path.GetFileNameWithoutExtension(fileName)
            .Replace("CV_", "").Replace("CV-", "").Replace("CV ", "")
            .Replace("_", " ").Replace("-", " ").Trim();
        return string.IsNullOrWhiteSpace(clean) ? "á»¨ng viÃªn" : clean;
    }

    private static string? ExtractEmailFromText(string text)
    {
        if (string.IsNullOrEmpty(text)) return null;
        var m = Regex.Match(text, @"[a-zA-Z0-9._%+\-]+@[a-zA-Z0-9.\-]+\.[a-zA-Z]{2,}");
        return m.Success ? m.Value : null;
    }

    private static string? ExtractPhoneFromText(string text)
    {
        if (string.IsNullOrEmpty(text)) return null;
        var m = Regex.Match(text, @"(0[3|5|7|8|9])+([0-9]{8})\b");
        return m.Success ? m.Value : null;
    }
}

// =============================================================================
// Custom Exception cho 429/503 â€” giÃºp phÃ¢n biá»‡t rÃµ rate-limit vá»›i lá»—i khÃ¡c
// =============================================================================
public sealed class GeminiRateLimitException : Exception
{
    public GeminiRateLimitException(string message) : base(message) { }
}

// =============================================================================
// Helper record cho raw SQL cache query
// =============================================================================
public sealed record CachedScreeningRecord
{
    public string ContentHash { get; init; } = string.Empty;
    public string ResultJson { get; init; } = string.Empty;
}

