using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using UTC_DATN.Models;
using UTC_DATN.Data;
using UTC_DATN.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using UTC_DATN.Services.Interfaces;
using UTC_DATN.Services.Implements;

namespace UTC_DATN.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatbotController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<ChatbotController> _logger;
        private readonly IConfiguration _configuration;
        private readonly UTC_DATNContext _context;
        private readonly IMemoryCache _cache;
        private readonly IGeminiApiKeyProvider _apiKeyProvider;
        private readonly IntentClassifier _intentClassifier;
        private readonly EntityExtractor _entityExtractor;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly UTC_DATN.Services.Background.IBackgroundTaskQueue _taskQueue;

        public ChatbotController(
            IHttpClientFactory httpClientFactory, 
            ILogger<ChatbotController> logger, 
            IConfiguration configuration,
            UTC_DATNContext context,
            IMemoryCache cache,
            IGeminiApiKeyProvider apiKeyProvider,
            IntentClassifier intentClassifier,
            EntityExtractor entityExtractor,
            IServiceScopeFactory scopeFactory,
            UTC_DATN.Services.Background.IBackgroundTaskQueue taskQueue)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _configuration = configuration;
            _context = context;
            _cache = cache;
            _apiKeyProvider = apiKeyProvider;
            _intentClassifier = intentClassifier;
            _entityExtractor = entityExtractor;
            _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
            _taskQueue = taskQueue ?? throw new ArgumentNullException(nameof(taskQueue));
        }

        [HttpGet("admin/faqs")]
        [Authorize(Roles = "ADMIN,HR")]
        public async Task<IActionResult> GetFaqs([FromQuery] string? search = null, [FromQuery] bool? isActive = null)
        {
            var query = _context.ChatbotFaqs.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var keyword = search.Trim().ToLower();
                query = query.Where(x => x.Question.ToLower().Contains(keyword)
                    || x.Answer.ToLower().Contains(keyword)
                    || (x.Keywords != null && x.Keywords.ToLower().Contains(keyword))
                    || x.Category.ToLower().Contains(keyword));
            }

            if (isActive.HasValue)
            {
                query = query.Where(x => x.IsActive == isActive.Value);
            }

            var items = await query
                .OrderByDescending(x => x.IsActive)
                .ThenByDescending(x => x.Priority)
                .ThenBy(x => x.Question)
                .Select(x => new ChatbotFaqAdminDto
                {
                    FaqId = x.FaqId,
                    Question = x.Question,
                    Answer = x.Answer,
                    Category = x.Category,
                    Keywords = x.Keywords,
                    Priority = x.Priority,
                    IsActive = x.IsActive,
                    CreatedAt = x.CreatedAt,
                    UpdatedAt = x.UpdatedAt
                })
                .ToListAsync();

            return Ok(items);
        }

        [HttpPost("admin/faqs")]
        [Authorize(Roles = "ADMIN,HR")]
        public async Task<IActionResult> CreateFaq([FromBody] UpsertChatbotFaqDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Question) || string.IsNullOrWhiteSpace(dto.Answer))
            {
                return BadRequest(new { message = "Question và Answer là bắt buộc." });
            }

            var faq = new ChatbotFaq
            {
                FaqId = Guid.NewGuid(),
                Question = dto.Question.Trim(),
                Answer = dto.Answer.Trim(),
                Category = string.IsNullOrWhiteSpace(dto.Category) ? "General" : dto.Category.Trim(),
                Keywords = NormalizeKeywords(dto.Keywords),
                Priority = dto.Priority,
                IsActive = dto.IsActive,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = GetCurrentUserIdOrNull()
            };

            _context.ChatbotFaqs.Add(faq);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Tạo FAQ thành công.", faqId = faq.FaqId });
        }

        [HttpPut("admin/faqs/{faqId:guid}")]
        [Authorize(Roles = "ADMIN,HR")]
        public async Task<IActionResult> UpdateFaq(Guid faqId, [FromBody] UpsertChatbotFaqDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Question) || string.IsNullOrWhiteSpace(dto.Answer))
            {
                return BadRequest(new { message = "Question và Answer là bắt buộc." });
            }

            var faq = await _context.ChatbotFaqs.FirstOrDefaultAsync(x => x.FaqId == faqId);
            if (faq == null)
            {
                return NotFound(new { message = "Không tìm thấy FAQ." });
            }

            faq.Question = dto.Question.Trim();
            faq.Answer = dto.Answer.Trim();
            faq.Category = string.IsNullOrWhiteSpace(dto.Category) ? "General" : dto.Category.Trim();
            faq.Keywords = NormalizeKeywords(dto.Keywords);
            faq.Priority = dto.Priority;
            faq.IsActive = dto.IsActive;
            faq.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Cập nhật FAQ thành công." });
        }

        [HttpPatch("admin/faqs/{faqId:guid}/toggle")]
        [Authorize(Roles = "ADMIN,HR")]
        public async Task<IActionResult> ToggleFaq(Guid faqId)
        {
            var faq = await _context.ChatbotFaqs.FirstOrDefaultAsync(x => x.FaqId == faqId);
            if (faq == null)
            {
                return NotFound(new { message = "Không tìm thấy FAQ." });
            }

            faq.IsActive = !faq.IsActive;
            faq.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new { message = faq.IsActive ? "Đã bật FAQ." : "Đã tắt FAQ.", isActive = faq.IsActive });
        }

        [HttpDelete("admin/faqs/{faqId:guid}")]
        [Authorize(Roles = "ADMIN,HR")]
        public async Task<IActionResult> DeleteFaq(Guid faqId)
        {
            var faq = await _context.ChatbotFaqs.FirstOrDefaultAsync(x => x.FaqId == faqId);
            if (faq == null)
            {
                return NotFound(new { message = "Không tìm thấy FAQ." });
            }

            _context.ChatbotFaqs.Remove(faq);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Đã xóa FAQ." });
        }

        [HttpPost("ask")]
        public async Task Ask([FromBody] ChatRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.Message))
            {
                await WriteStreamEvent(Response, "Vui lòng nhập lời nhắn của bạn.");
                return;
            }

            _logger.LogInformation($"[Chatbot] Nhận yêu cầu: {request.Message}");

            try
            {
                // BẮT BUỘC: Đặt Timeout tối đa lên 120s dể phòng trường hợp AI sinh text lên tới 5 trang giấy
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(120));

                // Lớp trả lời nhanh: ưu tiên FAQ trước khi gọi LLM để giảm độ "tù" và giảm token.
                var faqAnswer = await TryGetFaqAnswerAsync(request.Message, cts.Token);
                if (!string.IsNullOrWhiteSpace(faqAnswer))
                {
                    await WriteStreamEvent(Response, faqAnswer);
                    return;
                }

                var sw = Stopwatch.StartNew();
                var httpClient = _httpClientFactory.CreateClient("GeminiClient");

                // Ensure we have a session id and persist the user message
                var sessionId = request.SessionId ?? Guid.NewGuid();
                var savedUserMessage = await SaveUserMessageAsync(request.Message, sessionId, cts.Token);

                var apiKey = _apiKeyProvider.GetApiKey("Chatbot");
                if (string.IsNullOrEmpty(apiKey))
                {
                    _logger.LogWarning("Gemini API Key is not configured.");
                    await WriteStreamEvent(Response, "Lỗi Server: Chưa cấu hình Gemini API Key.");
                    return;
                }

                // 1. Phân luồng dữ liệu (RAG Keyword Router)
                var msgLower = request.Message.ToLower();
                // Nới lỏng Regex: Chỉ cần chứa từ khóa ngành nghề là kích hoạt RAG để AI luôn có dữ liệu thực tế
                var isJobQuery = Regex.IsMatch(msgLower, @"(việc|job|công việc|intern|thực tập|tuyển|vị trí|developer|lập trình|sinh viên|phần mềm)");
                var isSkillQuery = Regex.IsMatch(msgLower, @"(kỹ năng|skill|ngôn ngữ|framework|công nghệ|yêu cầu)");
                // Nới lỏng Regex: Chấp nhận các câu chào ngắn gọn (dưới 30 ký tự)
                var isSmallTalk = request.Message.Length < 30 && Regex.IsMatch(msgLower, @"(hi|hello|chào|xin chào|ê|alo|bye|tạm biệt|hi there|hey|tư vấn)");

                string systemPrompt = "";

                if (isSmallTalk)
                {
                    Response.ContentType = "text/event-stream";
                    Response.Headers["Cache-Control"] = "no-cache";
                    Response.Headers["Connection"] = "keep-alive";

                    var greetingMsg = "Chào bạn! Tôi là V9 Assistant - AI tư vấn tuyển dụng độc quyền của V9 TECH. Tôi có thể giúp bạn tìm kiếm thông tin Việc làm, Kỹ năng IT, hoặc tư vấn kinh nghiệm phỏng vấn. Bạn đang quan tâm đến vị trí nào?";
                    var words = greetingMsg.Split(' ');
                    foreach (var word in words)
                    {
                        var chunkObj = new { text = word + " " };
                        await Response.WriteAsync($"data: {JsonSerializer.Serialize(chunkObj)}\n\n", cts.Token);
                        await Response.Body.FlushAsync(cts.Token);
                        await Task.Delay(30); 
                    }
                    await Response.WriteAsync("data: [DONE]\n\n", cts.Token);
                    await Response.Body.FlushAsync(cts.Token);
                    return; 
                }
                else
                {
                    systemPrompt = $@"Bạn là V9 Assistant - AI tư vấn tuyển dụng ĐỘC QUYỀN của V9 TECH. 
Hôm nay là {DateTime.Now:dd/MM/yyyy HH:mm:ss} (Năm {DateTime.Now.Year}).

=== PERSONA & TONE ===
Bạn là một chuyên gia tuyển dụng IT thân thiện, chuyên nghiệp & sáng tạo:
• Giải thích job descriptions một cách dễ hiểu, friendly
• Có thể suggest variations/alternatives khi user hỏi về jobs khác nhau
• Proactive suggest tips/advice liên quan (CV tips, interview prep, etc.)
• Encouraging & supportive, giúp candidate self-assess fit

=== SCOPE & GUARDRAILS ===
CHỈ ĐƯỢC PHÉP trả lời về:
✓ Vị trí tuyển dụng, yêu cầu, career path
✓ Kỹ năng IT, công nghệ trending, learning roadmap
✓ Quy trình ứng tuyển, phỏng vấn, offer
✓ Công ty V9 TECH, culture, benefits
✓ CV/LinkedIn tips, interview preparation

NẾU user hỏi ngoài scope (thời tiết, nấu ăn, toán học, thần thoại, code dạo, etc.):
→ TỪ CHỐI LỊCH SỰ: 'Xin lỗi, tôi chỉ hỗ trợ về tuyển dụng & IT. Bạn có câu hỏi nào về công việc tại V9 TECH không?'
→ ĐƠN GIẢN, KHÔNG BÀO CHỮ, KHÔNG NGOÀI LỆ

=== JOB VARIATIONS & EXPLANATIONS ===
Khi user hỏi về jobs, TỰ ĐỘNG:
1. Giải thích job description đơn giản hơn
2. Suggest vị trí similar khác (Frontend → Full-stack alternative)
3. Explain progression (Junior → Middle path)
4. Clarify requirements vs nice-to-have
Ví dụ: User hỏi 'Backend Developer khó không?'
→ Explain role, compare với Frontend, suggest roadmap, then ask 'Bạn prefer Backend hay muốn explore Frontend?'

=== INTERACTION STYLE ===
• SHORT-FORM answers (2-4 sentences for quick q, bullet points for lists)
• ASK CLARIFYING QUESTIONS để hiểu user motivation
• PROVIDE CONCRETE EXAMPLES từ V9 TECH hoặc industry
• AVOID generic AI-speak, use natural Vietnamese

=== CONTEXT AWARENESS ===
• Remember user profile từ conversation history nếu có
• Adapt advice based on seniority level (Junior vs Senior questions differ)
• Suggest relevant next steps (e.g., 'Next bạn có thể...', 'Gợi ý...')";
                }

                if (isJobQuery)
                {
                    // Lấy Real-time job details (not just titles) để AI có full context
                    var utcNow = DateTime.UtcNow;
                    var activeJobs = await _context.Jobs
                        .Where(j => j.Status == "OPEN" && !j.IsDeleted && (j.Deadline == null || j.Deadline >= utcNow))
                        .OrderByDescending(j => j.CreatedAt)
                        .Take(8) // Giảm xuống 8 để tiết kiệm token, lấy full details thay vì chỉ title
                        .Select(j => new { j.Title, j.Description, j.Requirements, j.SalaryMin, j.SalaryMax, j.Currency })
                        .ToListAsync(cts.Token);
                    
                    _logger.LogInformation($"[Chatbot] Đã tìm thấy {activeJobs.Count} jobs với full details cho RAG.");
                    
                    // Format jobs data để AI dễ parse
                    var jobsContext = activeJobs.Any()
                        ? "=== ACTIVE JOBS AT V9 TECH ===\n" + string.Join("\n---\n", activeJobs.Select((j, idx) =>
                            $"Job #{idx + 1}: {j.Title}\nDescription: {(j.Description?.Length > 200 ? j.Description.Substring(0, 200) + "..." : j.Description)}\nRequirements: {(j.Requirements?.Length > 200 ? j.Requirements.Substring(0, 200) + "..." : j.Requirements)}\nSalary: {j.SalaryMin?.ToString("N0") ?? "?"}-{j.SalaryMax?.ToString("N0") ?? "?"} {j.Currency ?? "VNĐ"}/month"))
                        : "Không có job nào tuyển dụng lúc này.";
                    
                    systemPrompt += $"\n{jobsContext}\n\nKhi user hỏi về jobs:\n• Giải thích rõ requirement & benefits\n• Suggest job alternatives nếu cần\n• Ask user's interest để recommend fit role\n• Avoid paste URLs - thay vào đó nhắc user tìm tên job trên website";
                }
                else if (isSkillQuery)
                {
                    // Cache kỹ năng tận 60 phút vì nó hiếm khi thay đổi
                    if (!_cache.TryGetValue("Chatbot_TopSkills", out List<string>? skills) || skills == null)
                    {
                        // Lấy top các kỹ năng chưa bị khóa
                         skills = await _context.Skills
                            .Where(s => !s.IsDeleted)
                            .Take(15)
                            .Select(s => s.Name)
                            .ToListAsync(cts.Token);

                        var cacheEntryOptions = new MemoryCacheEntryOptions()
                             .SetAbsoluteExpiration(TimeSpan.FromMinutes(60));
                         _cache.Set("Chatbot_TopSkills", skills, cacheEntryOptions);
                    }
                    
                    var skillsStr = skills.Any() ? string.Join(", ", skills) : "Chưa cập nhật kỹ năng.";
                    systemPrompt += $"\nDanh sách Công nghệ/Kỹ năng công ty hay dùng:\n{skillsStr}";
                }

                // 2. Chuẩn bị Payload chuẩn Google (Dùng system_instruction để tách rào chắn ra token riêng)
                var contentsList = new List<object>();
                
                // Thêm lịch sử (2 câu gần nhất - đủ để AI hiểu ngữ cảnh mà không gây nặng Quota)
                if (request.History != null && request.History.Any())
                {
                    foreach (var h in request.History.TakeLast(2))
                    {
                        if (string.IsNullOrWhiteSpace(h.Content)) continue;
                        contentsList.Add(new 
                        { 
                            role = h.Role.ToLower() == "bot" ? "model" : "user", 
                            parts = new[] { new { text = h.Content } } 
                        });
                    }
                }

                // Tin nhắn hiện tại (SẠCH HOÀN TOÀN - không có system prompt nhồi vào)
                contentsList.Add(new 
                { 
                    role = "user", 
                    parts = new[] { new { text = request.Message } } 
                });

                // system_instruction: Tách rào chắn thành token riêng (Cách Google khuyến nghị nhất)
                // Cần dùng Dictionary vì C# không cho phép gach_dưới trong anonymous type identifier
                var payloadObj = new Dictionary<string, object>
                {
                    ["system_instruction"] = new { parts = new[] { new { text = systemPrompt } } },
                    ["contents"] = contentsList.ToArray()
                };
                var payloadJson = JsonSerializer.Serialize(payloadObj);

                // 3. Khởi tạo SSE HTTP Headers trước khi gọi Google
                Response.ContentType = "text/event-stream";
                Response.Headers["Cache-Control"] = "no-cache";
                Response.Headers["Connection"] = "keep-alive";
                await Response.Body.FlushAsync(cts.Token);

                // Gửi sessionId về client ngay lập tức để frontend lưu lại (nếu client cần)
                try
                {
                    var sessObj = new { sessionId = sessionId };
                    await Response.WriteAsync($"data: {JsonSerializer.Serialize(sessObj)}\n\n", cts.Token);
                    await Response.Body.FlushAsync(cts.Token);
                }
                catch { }

                // 4. Hàm thực thi HTTP Call hỗ trợ Streaming với retry + per-key reporting
                async Task<HttpResponseMessage> CallGeminiWithRetry(string baseModelUrl, string json, string moduleName)
                {
                    var maxAttempts = 3;
                    var attempt = 0;
                    var delayMs = 1000;

                    while (attempt < maxAttempts && !cts.IsCancellationRequested)
                    {
                        attempt++;
                        string key;
                        try
                        {
                            key = _apiKeyProvider.GetApiKey(moduleName);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Lỗi khi lấy Gemini API key");
                            throw;
                        }

                        var urlWithKey = baseModelUrl + "&key=" + System.Net.WebUtility.UrlEncode(key);
                        try
                        {
                            var req = new HttpRequestMessage(HttpMethod.Post, urlWithKey)
                            {
                                Content = new StringContent(json, Encoding.UTF8, "application/json")
                            };

                            _logger.LogInformation("[Gemini] Gọi API (attempt {Attempt}) với key {KeyPreview}", attempt, TruncateKeyForLogs(key));
                            var resp = await httpClient.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, cts.Token);

                            if (resp.IsSuccessStatusCode)
                            {
                                _apiKeyProvider.ReportSuccess(key);
                                return resp;
                            }

                            // Report failure for 429/5xx
                            if (resp.StatusCode == System.Net.HttpStatusCode.TooManyRequests || (int)resp.StatusCode >= 500)
                            {
                                _apiKeyProvider.ReportFailure(key);
                                _logger.LogWarning("[Gemini] Non-success status {Status} on attempt {Attempt} (key preview: {KeyPreview})", resp.StatusCode, attempt, TruncateKeyForLogs(key));
                                resp.Dispose();

                                // jittered backoff
                                var jitter = new Random().Next(0, 300);
                                await Task.Delay(delayMs + jitter, cts.Token);
                                delayMs *= 2;
                                continue;
                            }

                            // Other non-success (4xx) - no retry
                            return resp;
                        }
                        catch (OperationCanceledException) when (cts.IsCancellationRequested)
                        {
                            throw;
                        }
                        catch (Exception ex)
                        {
                            _apiKeyProvider.ReportFailure(key);
                            _logger.LogWarning(ex, "Lỗi khi gọi Gemini trên attempt {Attempt}", attempt);
                            var jitter = new Random().Next(0, 300);
                            await Task.Delay(delayMs + jitter, cts.Token);
                            delayMs *= 2;
                            continue;
                        }
                    }

                    throw new HttpRequestException("Failed to call Gemini after multiple attempts");
                }

                var baseUrl = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:streamGenerateContent?alt=sse";
                _logger.LogInformation($"[Gemini 2.5] Gọi API... Tokens ước tính: {payloadJson.Length} chars.");
                HttpResponseMessage response;
                try
                {
                    response = await CallGeminiWithRetry(baseUrl, payloadJson, "Chatbot");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Gọi Gemini thất bại sau retry");
                    await WriteStreamEvent(Response, "Rất tiếc, hệ thống AI tạm thời không khả dụng. Vui lòng thử lại sau.");
                    return;
                }
                // 5. Đọc Stream liên tục và đẩy về luồng Console/Angular
                using var responseStream = await response.Content.ReadAsStreamAsync(cts.Token);
                using var reader = new StreamReader(responseStream);

                var assistantSb = new StringBuilder();

                while (!reader.EndOfStream && !cts.Token.IsCancellationRequested)
                {
                    var line = await reader.ReadLineAsync(cts.Token);
                    if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("data: ")) continue;

                    var jsonPayload = line.Substring("data: ".Length).Trim();
                    if (jsonPayload == "[DONE]") break; // Xong phiên chat

                    try 
                    {
                        using var document = JsonDocument.Parse(jsonPayload);
                        var candidates = document.RootElement.GetProperty("candidates");
                        if (candidates.GetArrayLength() > 0)
                        {
                            var contentProp = candidates[0].GetProperty("content");
                            if (contentProp.TryGetProperty("parts", out var parts) && parts.GetArrayLength() > 0)
                            {
                                if (parts[0].TryGetProperty("text", out var textElement))
                                {
                                    var textChunk = textElement.GetString();
                                    if (!string.IsNullOrEmpty(textChunk))
                                    {
                                        assistantSb.Append(textChunk);
                                        var chunkObj = new { text = textChunk };
                                        var chunkJson = JsonSerializer.Serialize(chunkObj);
                                        await Response.WriteAsync($"data: {chunkJson}\n\n", cts.Token);
                                        await Response.Body.FlushAsync(cts.Token);
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"Bỏ qua chunk JSON dị dạng từ Gemini: {ex.Message}");
                    }
                }
                
                // Kết thúc Stream bình thường, gửi cờ DONE để nhả Frontend Loader
                await Response.WriteAsync("data: [DONE]\n\n", cts.Token);
                await Response.Body.FlushAsync(cts.Token);

                // Save assistant full message to DB (non-blocking safety)
                try
                {
                    var assistantText = assistantSb.ToString();
                    if (!string.IsNullOrWhiteSpace(assistantText))
                    {
                        _taskQueue.QueueBackgroundWorkItem(ct => SaveAssistantMessageAsync(assistantText, sessionId, ct));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error saving assistant message");
                }
            }
            // Khối Fallback 
            catch (Exception ex) when (ex is TaskCanceledException || ex is HttpRequestException)
            {
                _logger.LogWarning(ex, "Gemini API Streaming Timeout/Error.");
                await WriteStreamEvent(Response, "Hệ thống đang quá tải hoặc kết nối bị gián đoạn. Bạn vui lòng thử lại sau nhé!");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi Server khi xử lý Chatbot Streaming.");
                await WriteStreamEvent(Response, "Có lỗi nội bộ xảy ra.");
            }
        }

        // Hàm tiện ích hỗ trợ nhả Text Event Stream một lần nếu bắt lỗi sớm
        private async Task WriteStreamEvent(HttpResponse response, string message)
        {
            if (!response.HasStarted)
            {
                response.ContentType = "text/event-stream";
                response.Headers["Cache-Control"] = "no-cache";
                response.Headers["Connection"] = "keep-alive";
            }
            var chunkObj = new { text = message };
            await response.WriteAsync($"data: {JsonSerializer.Serialize(chunkObj)}\n\n");
            await response.WriteAsync("data: [DONE]\n\n");
            await response.Body.FlushAsync();
        }

        private async Task<string?> TryGetFaqAnswerAsync(string message, CancellationToken cancellationToken)
        {
            var normalized = message.Trim().ToLower();
            if (string.IsNullOrWhiteSpace(normalized))
            {
                return null;
            }

            var candidates = await _context.ChatbotFaqs
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderByDescending(x => x.Priority)
                .ThenBy(x => x.Question)
                .Take(100)
                .ToListAsync(cancellationToken);

            foreach (var faq in candidates)
            {
                var question = faq.Question?.Trim().ToLower() ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(question)
                    && (normalized.Contains(question) || question.Contains(normalized)))
                {
                    return faq.Answer;
                }

                if (string.IsNullOrWhiteSpace(faq.Keywords))
                {
                    continue;
                }

                var keywords = faq.Keywords
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(x => x.ToLower())
                    .Where(x => x.Length >= 2);

                if (keywords.Any(k => normalized.Contains(k)))
                {
                    return faq.Answer;
                }
            }

            return null;
        }

        private static string NormalizeKeywords(string? rawKeywords)
        {
            if (string.IsNullOrWhiteSpace(rawKeywords))
            {
                return string.Empty;
            }

            var normalized = rawKeywords
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(x => x.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase);

            return string.Join(",", normalized);
        }

        private Guid? GetCurrentUserIdOrNull()
        {
            var claim = User?.Claims?.FirstOrDefault(c => c.Type == "sub"
                || c.Type == ClaimTypes.NameIdentifier
                || c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier");

            if (claim == null)
            {
                return null;
            }

            return Guid.TryParse(claim.Value, out var userId) ? userId : null;
        }

        private static string TruncateKeyForLogs(string key)
        {
            if (string.IsNullOrEmpty(key)) return "(empty)";
            if (key.Length <= 8) return key;
            return key.Substring(0, 4) + "..." + key.Substring(key.Length - 4);
        }

        /// <summary>
        /// Helper: Lưu user message vào database
        /// </summary>
        private async Task<ChatMessage?> SaveUserMessageAsync(string message, Guid sessionId, CancellationToken cancellationToken = default)
        {
            try
            {
                // If session row doesn't exist in DB (requires ApplicationId FK),
                // do not attempt to create a placeholder ChatSession here.
                // Instead leave ChatSessionId null so FK is not violated.
                var existingSession = await _context.ChatSessions.FindAsync(new object[] { sessionId }, cancellationToken);

                var chatMessage = new ChatMessage
                {
                    ChatMessageId = Guid.NewGuid(),
                    ChatSessionId = existingSession != null ? sessionId : (Guid?)null,
                    Sender = "user",
                    Message = message,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.ChatMessages.Add(chatMessage);
                await _context.SaveChangesAsync(cancellationToken);

                // Phân loại intent non-blocking: enqueue cho background worker
                _taskQueue.QueueBackgroundWorkItem(ct => ClassifyAndSaveAnalyticsAsync(chatMessage, ct));

                return chatMessage;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving user message");
                return null;
            }
        }

        /// <summary>
        /// Helper: Phân loại intent và lưu analytics
        /// </summary>
        private async Task ClassifyAndSaveAnalyticsAsync(ChatMessage message, CancellationToken cancellationToken = default)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<UTC_DATNContext>();
                var intentClassifier = scope.ServiceProvider.GetRequiredService<IntentClassifier>();
                var entityExtractor = scope.ServiceProvider.GetRequiredService<EntityExtractor>();

                var sw = Stopwatch.StartNew();

                var intentResult = await intentClassifier.ClassifyAsync(message.Message);
                var entities = await entityExtractor.ExtractAsync(message.Message);

                sw.Stop();

                var analytics = new ChatAnalytics
                {
                    AnalyticsId = Guid.NewGuid(),
                    MessageId = message.ChatMessageId,
                    Intent = intentResult.Intent,
                    IntentConfidence = (decimal)intentResult.Confidence,
                    ResponseTimeMs = (int)sw.ElapsedMilliseconds,
                    EntityCount = entities.Count,
                    EntityConfidence = entities.Count > 0 ? (decimal)entities.Average(e => e.Confidence) : null,
                    CreatedAt = DateTime.UtcNow,
                    WasEscalated = false
                };

                db.ChatAnalytics.Add(analytics);
                await db.SaveChangesAsync(cancellationToken);

                _logger.LogDebug($"[Analytics] Saved for message {message.ChatMessageId}: Intent={intentResult.Intent} ({intentResult.Confidence:P})");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error classifying and saving analytics");
            }
        }

        /// <summary>
        /// Helper: Lưu assistant response message
        /// </summary>
        private async Task<ChatMessage?> SaveAssistantMessageAsync(string message, Guid sessionId, CancellationToken cancellationToken = default)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<UTC_DATNContext>();

                var existingSession = await db.ChatSessions.FindAsync(new object[] { sessionId }, cancellationToken);

                var chatMessage = new ChatMessage
                {
                    ChatMessageId = Guid.NewGuid(),
                    ChatSessionId = existingSession != null ? sessionId : (Guid?)null,
                    Sender = "assistant",
                    Message = message,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                db.ChatMessages.Add(chatMessage);
                await db.SaveChangesAsync(cancellationToken);

                return chatMessage;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving assistant message");
                return null;
            }
        }
    }

    public class UpsertChatbotFaqDto
    {
        public string Question { get; set; } = string.Empty;
        public string Answer { get; set; } = string.Empty;
        public string Category { get; set; } = "General";
        public string? Keywords { get; set; }
        public int Priority { get; set; } = 0;
        public bool IsActive { get; set; } = true;
    }

    public class ChatbotFaqAdminDto
    {
        public Guid FaqId { get; set; }
        public string Question { get; set; } = string.Empty;
        public string Answer { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string? Keywords { get; set; }
        public int Priority { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
