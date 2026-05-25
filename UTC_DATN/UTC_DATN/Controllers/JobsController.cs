using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using UTC_DATN.Data;
using UTC_DATN.DTOs.Job;
using UTC_DATN.Services.Interfaces;

namespace UTC_DATN.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class JobsController : ControllerBase
{
    private readonly IJobService _jobService;
    private readonly UTC_DATNContext _context;
    private readonly ILogger<JobsController> _logger;
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IGeminiApiKeyProvider _apiKeyProvider;

    public JobsController(
        IJobService jobService,
        UTC_DATNContext context,
        ILogger<JobsController> logger,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        IGeminiApiKeyProvider apiKeyProvider)
    {
        _jobService = jobService;
        _context = context;
        _logger = logger;
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
        _apiKeyProvider = apiKeyProvider;
    }

    /// <summary>
    /// API đăng tin tuyển dụng
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateJob([FromBody] CreateJobRequest request)
    {
        try
        {
            // Validate model
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Lấy UserId từ token
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            {
                return Unauthorized(new { message = "Không thể xác thực người dùng" });
            }

            // Gọi service
            var result = await _jobService.CreateJobAsync(request, userId);

            if (result)
            {
                return Ok(new { message = "Đăng tin tuyển dụng thành công" });
            }
            else
            {
                return BadRequest(new { message = "Đăng tin tuyển dụng thất bại" });
            }
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = $"Lỗi: {ex.Message}" });
        }
    }

    /// <summary>
    /// API cập nhật tin tuyển dụng
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateJob(Guid id, [FromBody] UpdateJobRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _jobService.UpdateJobAsync(id, request);

            if (result)
            {
                return Ok(new { message = "Cập nhật tin tuyển dụng thành công" });
            }
            else
            {
                return BadRequest(new { message = "Cập nhật thất bại hoặc không tìm thấy tin tuyển dụng" });
            }
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = $"Lỗi: {ex.Message}" });
        }
    }

    /// <summary>
    /// API xóa tin tuyển dụng
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteJob(Guid id)
    {
        try
        {
            var result = await _jobService.DeleteJobAsync(id);

            if (result)
            {
                return Ok(new { message = "Xóa tin tuyển dụng thành công" });
            }
            else
            {
                return BadRequest(new { message = "Xóa thất bại hoặc không tìm thấy tin tuyển dụng" });
            }
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = $"Lỗi: {ex.Message}" });
        }
    }

    /// <summary>
    /// API đóng tin tuyển dụng (Ngừng đăng)
    /// </summary>
    [HttpPut("{id}/close")]
    public async Task<IActionResult> CloseJob(Guid id)
    {
        try
        {
            // Lấy thông tin người thực hiện
            var closedByIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(closedByIdClaim, out Guid closedById))
                return Unauthorized();
            var fullName = User.FindFirst("FullName")?.Value ?? "User";
            var role = User.FindFirst(ClaimTypes.Role)?.Value ?? "Admin";
            var closedByName = $"{fullName} {role}";

            var result = await _jobService.CloseJobAsync(id, closedById, closedByName);

            if (result)
                return Ok(new { message = "Dã ngưng đăng tin tuyển dụng" });
            else
                return BadRequest(new { message = "Thao tác thất bại hoặc không tìm thấy tin tuyển dụng" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = $"Lỗi: {ex.Message}" });
        }
    }

    /// <summary>
    /// API mở lại tin tuyển dụng
    /// </summary>
    [HttpPut("{id}/open")]
    public async Task<IActionResult> OpenJob(Guid id)
    {
        try
        {
            var result = await _jobService.OpenJobAsync(id);

            if (result)
            {
                return Ok(new { message = "Đã mở lại tin tuyển dụng" });
            }
            else
            {
                return BadRequest(new { message = "Thao tác thất bại hoặc không tìm thấy tin tuyển dụng" });
            }
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = $"Lỗi: {ex.Message}" });
        }
    }

    /// <summary>
    /// API lấy danh sách tất cả job (cho admin/HR quản lý)
    /// </summary>
    [HttpGet]
    // [Authorize(Roles = "ADMIN,HR")] 
    public async Task<IActionResult> GetAllJobs()
    {
        try
        {
            var jobs = await _jobService.GetAllJobsAsync();
            return Ok(jobs);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = $"Lỗi: {ex.Message}" });
        }
    }

    /// <summary>
    /// API lấy danh sách job mới nhất cho trang chủ
    /// Hỗ trợ tìm kiếm theo keyword (title, company, skills) và location
    /// Nếu user đã đăng nhập, sẽ include thông tin đã ứng tuyển (HasApplied, AppliedAt)
    /// </summary>
    [HttpGet("latest/{count}")]
    [AllowAnonymous] // Cho phép truy cập không cần token
    public async Task<IActionResult> GetLatestJobs(
        int count = 10,
        [FromQuery] string? keyword = null,
        [FromQuery] string? location = null)
    {
        try
        {
            var jobs = await _jobService.GetLatestJobsAsync(count, keyword, location);
            
            // Nếu user đã đăng nhập, thêm thông tin đã ứng tuyển
            var token = Request.Headers["Authorization"].ToString();
            if (!string.IsNullOrEmpty(token))
            {
                try
                {
                    var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                    if (!string.IsNullOrEmpty(userId) && Guid.TryParse(userId, out var userGuid))
                    {
                        var candidate = await _context.Candidates.FirstOrDefaultAsync(c => c.UserId == userGuid);
                        if (candidate != null)
                        {
                            var appliedJobs = await _context.Applications
                                .Where(a => a.CandidateId == candidate.CandidateId)
                                .Select(a => new { a.JobId, a.AppliedAt })
                                .ToListAsync();

                            var appliedJobDictionary = appliedJobs.ToDictionary(a => a.JobId, a => a.AppliedAt);

                            foreach (var job in jobs)
                            {
                                if (appliedJobDictionary.TryGetValue(job.JobId, out var appliedAt))
                                {
                                    job.HasApplied = true;
                                    job.AppliedAt = appliedAt;
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Lỗi khi lấy thông tin đã ứng tuyển");
                }
            }

            return Ok(jobs);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = $"Lỗi: {ex.Message}" });
        }
    }

    /// <summary>
    /// API lấy chi tiết job theo ID
    /// </summary>
    [HttpGet("{id}")]
    [AllowAnonymous] 
    public async Task<IActionResult> GetJobById(Guid id)
    {
        try
        {
            var job = await _jobService.GetJobByIdAsync(id);
            
            if (job == null)
            {
                return NotFound(new { message = "Không tìm thấy công việc" });
            }
            
            return Ok(job);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = $"Lỗi: {ex.Message}" });
        }
    }

    /// <summary>
    /// AI sinh nội dung Job Description (description, requirements, benefits)
    /// dựa trên tiêu đề, cấp độ, yêu cầu chính và mức lương
    /// </summary>
    [HttpPost("ai-generate-jd")]
    [Authorize(Roles = "ADMIN,HR")]
    public async Task<IActionResult> AiGenerateJd([FromBody] AiGenerateJdRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            return BadRequest(new { message = "Tiêu đề vị trí là bắt buộc." });

        var apiKey = _apiKeyProvider.GetApiKey("JD");
        if (string.IsNullOrEmpty(apiKey))
            return StatusCode(503, new { message = "Chưa cấu hình Gemini API Key." });

        _logger.LogInformation("[AI JD] Generating JD for: {Title} / {Level}", request.Title, request.Level);

        var salaryText = (request.SalaryMin.HasValue || request.SalaryMax.HasValue)
            ? $"{request.SalaryMin?.ToString("N0") ?? "?"} – {request.SalaryMax?.ToString("N0") ?? "?"} {request.Currency ?? "VNĐ"}/tháng"
            : "Thỏa thuận";

        var prompt = $@"Bạn là chuyên gia tuyển dụng IT chuyên nghiệp tại Việt Nam.
Hãy viết Job Description cho vị trí sau:

- Tên vị trí: {request.Title}
- Cấp độ: {(string.IsNullOrWhiteSpace(request.Level) ? "Không xác định" : request.Level)}
- Yêu cầu chính: {(string.IsNullOrWhiteSpace(request.KeyRequirements) ? "Không có" : request.KeyRequirements)}
- Mức lương: {salaryText}

Yêu cầu:
1. Viết bằng tiếng Việt, chuyên nghiệp, hấp dẫn ứng viên IT
2. description: 3-4 đoạn giới thiệu công việc, môi trường làm việc (150-200 từ)
3. requirements: danh sách bullet points các yêu cầu kỹ thuật và kinh nghiệm (8-12 điểm)
4. benefits: danh sách bullet points quyền lợi hấp dẫn (6-8 điểm)

CHỈ trả về JSON thuần (không markdown, không code block):
{{
  ""description"": ""<nội dung mô tả>"",
  ""requirements"": ""<danh sách yêu cầu, mỗi điểm trên 1 dòng bắt đầu bằng • >"",
  ""benefits"": ""<danh sách quyền lợi, mỗi điểm trên 1 dòng bắt đầu bằng • >""
}}";

        try
        {
            var httpClient = _httpClientFactory.CreateClient("GeminiClient");
            var payload = new
            {
                contents = new[]
                {
                    new { parts = new[] { new { text = prompt } } }
                }
            };

            var jsonContent = new StringContent(
                JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            // Thử với 3 API keys (original + 2 fallback)
            HttpResponseMessage response = null;
            for (int attempt = 0; attempt < 3; attempt++)
            {
                var currentKey = attempt == 0 ? apiKey : _apiKeyProvider.GetNextApiKey();
                var apiUrl = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={currentKey}";
                
                _logger.LogInformation("[AI JD] Attempt {Attempt} with key", attempt + 1);
                response = await httpClient.PostAsync(apiUrl, jsonContent);

                // Thành công, thoát vòng lặp
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("[AI JD] Success with key on attempt {Attempt}", attempt + 1);
                    break;
                }

                // 503 = rate limited, thử key khác
                if (response.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable && attempt < 2)
                {
                    _logger.LogWarning("[AI JD] Got 503, retrying with different key...");
                    continue;
                }

                // Lỗi khác, báo cáo
                var errBody = await response.Content.ReadAsStringAsync();
                _logger.LogError("[AI JD] Gemini error {Status}: {Body}", response.StatusCode, errBody);
                return StatusCode(502, new { message = "Gemini AI tạm thời không khả dụng. Vui lòng thử lại sau." });
            }

            if (!response.IsSuccessStatusCode)
            {
                return StatusCode(502, new { message = "Gemini AI tạm thời không khả dụng sau 3 lần thử." });
            }

            var responseText = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(responseText);
            var rawText = doc.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString() ?? "";

            // Làm sạch markdown nếu Gemini vẫn trả về
            rawText = rawText.Trim();
            if (rawText.StartsWith("```json")) rawText = rawText[7..];
            if (rawText.StartsWith("```")) rawText = rawText[3..];
            if (rawText.EndsWith("```")) rawText = rawText[..^3];
            rawText = rawText.Trim();

            using var resultDoc = JsonDocument.Parse(rawText);
            var result = new
            {
                description = resultDoc.RootElement.GetProperty("description").GetString() ?? "",
                requirements = resultDoc.RootElement.GetProperty("requirements").GetString() ?? "",
                benefits = resultDoc.RootElement.GetProperty("benefits").GetString() ?? ""
            };

            _logger.LogInformation("[AI JD] Generated successfully for: {Title}", request.Title);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AI JD] Error generating JD for {Title}", request.Title);
            return StatusCode(500, new { message = "Lỗi khi sinh nội dung JD. Vui lòng thử lại." });
        }
    }
}

public class AiGenerateJdRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Level { get; set; }
    public string? KeyRequirements { get; set; }
    public decimal? SalaryMin { get; set; }
    public decimal? SalaryMax { get; set; }
    public string? Currency { get; set; }
}
