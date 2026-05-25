using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using UTC_DATN.Data;
using UTC_DATN.Entities;
using UTC_DATN.Services.Interfaces;

namespace UTC_DATN.Controllers;

/// <summary>
/// Controller xử lý các API liên quan đến Interview và Email
/// </summary>
[ApiController]
[Route("api/interviews")]
[Authorize]
public class InterviewController : ControllerBase
{
    private readonly IAiMatchingService _aiMatchingService;
    private readonly IEmailService _emailService;
    private readonly ILogger<InterviewController> _logger;
    private readonly IInterviewService _interviewService;
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly UTC_DATNContext _context;
    private readonly IGeminiApiKeyProvider _apiKeyProvider;

    public InterviewController(
        IAiMatchingService aiMatchingService,
        IEmailService emailService,
        ILogger<InterviewController> logger,
        IInterviewService interviewService,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        UTC_DATNContext context,
        IGeminiApiKeyProvider apiKeyProvider)
    {
        _aiMatchingService = aiMatchingService;
        _emailService = emailService;
        _logger = logger;
        _interviewService = interviewService;
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
        _context = context;
        _apiKeyProvider = apiKeyProvider;
    }

    /// <summary>
    /// Sinh đoạn mở đầu email mời phỏng vấn (Draft)
    /// </summary>
    [HttpPost("generate-opening")]
    public async Task<IActionResult> GenerateOpening([FromBody] GenerateOpeningRequest request)
    {
        try
        {
            _logger.LogInformation("📝 API GenerateOpening - CandidateId: {CandidateId}, JobId: {JobId}", 
                request.CandidateId, request.JobId);

            if (request.CandidateId == Guid.Empty || request.JobId == Guid.Empty)
            {
                return BadRequest(new { message = "CandidateId và JobId không được để trống" });
            }

            var opening = await _aiMatchingService.GenerateInterviewOpeningAsync(request.CandidateId, request.JobId);

            return Ok(new { opening });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Không tìm thấy dữ liệu");
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi sinh đoạn mở đầu email");
            return StatusCode(500, new { message = "Có lỗi xảy ra khi sinh nội dung email" });
        }
    }

    /// <summary>
    /// Sinh toàn bộ nội dung email từ chối (Draft)
    /// </summary>
    [HttpPost("generate-rejection")]
    public async Task<IActionResult> GenerateRejection([FromBody] GenerateRejectionRequest request)
    {
        try
        {
            _logger.LogInformation("📝 API GenerateRejection - CandidateName: {CandidateName}, JobTitle: {JobTitle}", 
                request.CandidateName, request.JobTitle);

            if (string.IsNullOrWhiteSpace(request.CandidateName) || string.IsNullOrWhiteSpace(request.JobTitle))
            {
                return BadRequest(new { message = "CandidateName và JobTitle không được để trống" });
            }

            var body = await _aiMatchingService.GenerateRejectionEmailAsync(
                request.CandidateName, 
                request.JobTitle, 
                request.Reasons ?? new List<string>(), 
                request.Note ?? "");

            return Ok(new { body });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi sinh email từ chối");
            return StatusCode(500, new { message = "Có lỗi xảy ra khi sinh nội dung email" });
        }
    }

    /// <summary>
    /// API gửi email thủ công (Send - sau khi HR đã review/edit)
    /// </summary>
    [HttpPost("send-email-manual")]
    public async Task<IActionResult> SendEmailManual([FromBody] SendEmailManualRequest request)
    {
        try
        {
            _logger.LogInformation("📧 API SendEmailManual - ToEmail: {ToEmail}, Subject: {Subject}", 
                request.ToEmail, request.Subject);

            if (string.IsNullOrWhiteSpace(request.ToEmail))
                return BadRequest(new { message = "Email người nhận không được để trống" });
            if (string.IsNullOrWhiteSpace(request.Subject))
                return BadRequest(new { message = "Tiêu đề email không được để trống" });
            if (string.IsNullOrWhiteSpace(request.BodyHtml))
                return BadRequest(new { message = "Nội dung email không được để trống" });

            await _emailService.SendEmailAsync(request.ToEmail, request.Subject, request.BodyHtml);

            return Ok(new { message = "Email đã được gửi thành công!" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi gửi email thủ công");
            return StatusCode(500, new { message = "Có lỗi xảy ra khi gửi email: " + ex.Message });
        }
    }

    /// <summary>
    /// API lấy lịch phỏng vấn cá nhân
    /// </summary>
    [HttpGet("my-schedule")]
    [Authorize(Roles = "INTERVIEWER, HR, ADMIN")]
    public async Task<IActionResult> GetMySchedule()
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var currentUserId))
                return Unauthorized(new { success = false, message = "Vui lòng đăng nhập để xem lịch phỏng vấn." });

            var interviews = await _interviewService.GetMyInterviewScheduleAsync(currentUserId);
            return Ok(new { success = true, data = interviews });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting my interview schedule");
            return StatusCode(500, new { success = false, message = "Có lỗi xảy ra khi lấy lịch phỏng vấn." });
        }
    }

    /// <summary>
    /// AI sinh bộ câu hỏi phỏng vấn từ Job Description và lưu vào QuestionBank
    /// </summary>
    [HttpPost("ai-generate-questions")]
    [Authorize(Roles = "ADMIN,HR")]
    public async Task<IActionResult> AiGenerateQuestions([FromBody] AiGenerateQuestionsRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.JobDescription) && request.JobId == null)
            return BadRequest(new { message = "Cần cung cấp JobId hoặc JobDescription." });

        var apiKey = _apiKeyProvider.GetApiKey("Interview");
        if (string.IsNullOrEmpty(apiKey))
            return StatusCode(503, new { message = "Chưa cấu hình Gemini API Key." });

        // Lấy JD từ DB nếu có JobId
        string jobDescription = request.JobDescription ?? "";
        string jobTitle = request.JobTitle ?? "Không xác định";

        if (request.JobId.HasValue)
        {
            var job = await _context.Jobs
                .AsNoTracking()
                .FirstOrDefaultAsync(j => j.JobId == request.JobId.Value && !j.IsDeleted);
            if (job == null)
                return NotFound(new { message = "Không tìm thấy Job." });
            jobDescription = $"{job.Description}\n\nYêu cầu:\n{job.Requirements}";
            jobTitle = job.Title;
        }

        var count = Math.Clamp(request.Count, 5, 20);
        var level = string.IsNullOrWhiteSpace(request.Level) ? "Middle" : request.Level;
        var questionType = string.IsNullOrWhiteSpace(request.QuestionType) ? "Kỹ thuật" : request.QuestionType;

        _logger.LogInformation("[AI Questions] Generating {Count} questions for: {Title} / {Level}", count, jobTitle, level);

        var prompt = $@"Bạn là Tech Lead chuyên phỏng vấn tuyển dụng IT. Hãy sinh {count} câu hỏi phỏng vấn loại ""{questionType}"" cho vị trí {jobTitle} cấp độ {level}.

Mô tả công việc và yêu cầu:
{jobDescription}

Quy tắc:
- Câu hỏi phải bám sát vào yêu cầu kỹ thuật trong JD
- Độ khó phải phù hợp với cấp độ {level}
- Mỗi câu hỏi có câu trả lời mẫu ngắn gọn (2-3 câu chốt ý)
- Tags phải là tên công nghệ/kỹ năng cụ thể (VD: ""React"", ""SQL"", ""OOP"")

CHỈ trả về JSON array thuần (không markdown):
[
  {{
    ""content"": ""<nội dung câu hỏi>"",
    ""difficulty"": ""Easy|Medium|Hard"",
    ""explanation"": ""<câu trả lời mẫu ngắn gọn>"",
    ""tags"": [""<tag1>"", ""<tag2>""]
  }}
]";

        try
        {
            var httpClient = _httpClientFactory.CreateClient("GeminiClient");
            var payload = new
            {
                contents = new[] { new { parts = new[] { new { text = prompt } } } }
            };
            var jsonContent = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var apiUrl = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={apiKey}";

            var response = await httpClient.PostAsync(apiUrl, jsonContent);
            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync();
                _logger.LogError("[AI Questions] Gemini error {Status}: {Body}", response.StatusCode, err);
                return StatusCode(502, new { message = "Gemini AI tạm thời không khả dụng." });
            }

            var responseText = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseText);
            var rawText = doc.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString() ?? "[]";

            // Làm sạch markdown
            rawText = rawText.Trim();
            if (rawText.StartsWith("```json")) rawText = rawText[7..];
            if (rawText.StartsWith("```")) rawText = rawText[3..];
            if (rawText.EndsWith("```")) rawText = rawText[..^3];
            rawText = rawText.Trim();

            using var arrayDoc = JsonDocument.Parse(rawText);
            if (arrayDoc.RootElement.ValueKind != JsonValueKind.Array)
                return StatusCode(500, new { message = "AI trả về định dạng không hợp lệ." });

            // Lấy UserId từ token
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            Guid? createdBy = Guid.TryParse(userIdClaim, out var uid) ? uid : null;

            // Lưu vào QuestionBank
            var savedQuestions = new List<object>();
            foreach (var item in arrayDoc.RootElement.EnumerateArray())
            {
                var content = item.TryGetProperty("content", out var c) ? c.GetString() ?? "" : "";
                var difficulty = item.TryGetProperty("difficulty", out var d) ? d.GetString() ?? "Medium" : "Medium";
                var explanation = item.TryGetProperty("explanation", out var e) ? e.GetString() ?? "" : "";
                var tagsFromAi = item.TryGetProperty("tags", out var t) && t.ValueKind == JsonValueKind.Array
                    ? string.Join(",", t.EnumerateArray().Select(x => x.GetString() ?? ""))
                    : "";
                
                // Luôn gộp JobTitle vào tags để phân biệt câu hỏi của job nào
                var tags = string.IsNullOrWhiteSpace(tagsFromAi) 
                    ? jobTitle 
                    : $"{jobTitle},{tagsFromAi}";

                if (string.IsNullOrWhiteSpace(content)) continue;

                var question = new QuestionBank
                {
                    BankQuestionId = Guid.NewGuid(),
                    Content = content,
                    Type = questionType,
                    Difficulty = difficulty,
                    Explanation = explanation,
                    Tags = tags,
                    IsActive = true,
                    CreatedBy = createdBy,
                    CreatedAt = DateTime.UtcNow
                };
                _context.QuestionBanks.Add(question);
                savedQuestions.Add(new
                {
                    bankQuestionId = question.BankQuestionId,
                    content = question.Content,
                    difficulty = question.Difficulty,
                    explanation = question.Explanation,
                    tags = question.Tags
                });
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("[AI Questions] Saved {Count} questions to QuestionBank for: {Title}", savedQuestions.Count, jobTitle);
            return Ok(new
            {
                message = $"✅ Đã sinh và lưu {savedQuestions.Count} câu hỏi vào ngân hàng câu hỏi.",
                count = savedQuestions.Count,
                questions = savedQuestions
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AI Questions] Error generating questions");
            return StatusCode(500, new { message = "Lỗi khi sinh câu hỏi. Vui lòng thử lại." });
        }
    }

    /// <summary>
    /// Lấy danh sách QuestionBank (để xem sau khi sinh)
    /// </summary>
    [HttpGet("question-bank")]
    [Authorize(Roles = "ADMIN,HR,INTERVIEWER")]
    public async Task<IActionResult> GetQuestionBank(
        [FromQuery] string? search = null,
        [FromQuery] string? difficulty = null,
        [FromQuery] string? tags = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var query = _context.QuestionBanks.AsNoTracking().Where(q => q.IsActive);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(q => q.Content.Contains(search) || (q.Tags != null && q.Tags.Contains(search)));
        if (!string.IsNullOrWhiteSpace(difficulty))
            query = query.Where(q => q.Difficulty == difficulty);
        if (!string.IsNullOrWhiteSpace(tags))
            query = query.Where(q => q.Tags != null && q.Tags.Contains(tags));

        var totalCount = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var items = await query
            .OrderByDescending(q => q.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(q => new
            {
                q.BankQuestionId,
                q.Content,
                q.Type,
                q.Difficulty,
                q.Explanation,
                q.Tags,
                q.CreatedAt
            })
            .ToListAsync();

        return Ok(new
        {
            data = items,
            page,
            pageSize,
            totalCount,
            totalPages
        });
    }
}

#region DTOs

public class GenerateOpeningRequest
{
    public Guid CandidateId { get; set; }
    public Guid JobId { get; set; }
}

public class GenerateRejectionRequest
{
    public string CandidateName { get; set; } = string.Empty;
    public string JobTitle { get; set; } = string.Empty;
    public List<string>? Reasons { get; set; }
    public string? Note { get; set; }
}

public class SendEmailManualRequest
{
    public string ToEmail { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string BodyHtml { get; set; } = string.Empty;
}

public class AiGenerateQuestionsRequest
{
    public Guid? JobId { get; set; }
    public string? JobTitle { get; set; }
    public string? JobDescription { get; set; }
    public string? Level { get; set; }       // Junior, Middle, Senior, Lead
    public string? QuestionType { get; set; } // Kỹ thuật, Tình huống, Hành vi
    public int Count { get; set; } = 10;
}

#endregion
