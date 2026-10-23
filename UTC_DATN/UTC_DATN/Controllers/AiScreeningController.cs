using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UTC_DATN.DTOs.Ai;
using UTC_DATN.Services.Interfaces;

namespace UTC_DATN.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AiScreeningController : ControllerBase
{
    private readonly IAiScreeningService _aiScreeningService;
    private readonly ILogger<AiScreeningController> _logger;

    public AiScreeningController(
        IAiScreeningService aiScreeningService,
        ILogger<AiScreeningController> logger)
    {
        _aiScreeningService = aiScreeningService;
        _logger = logger;
    }

    /// <summary>
    /// Lấy danh sách chiến dịch tuyển dụng đang mở cho dropdown
    /// </summary>
    [HttpGet("campaigns")]
    public async Task<IActionResult> GetCampaigns()
    {
        try
        {
            var campaigns = await _aiScreeningService.GetActiveCampaignsAsync();
            return Ok(new { success = true, data = campaigns });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi lấy danh sách chiến dịch tuyển dụng");
            return StatusCode(500, new { success = false, message = "Không thể tải danh sách chiến dịch." });
        }
    }

    /// <summary>
    /// Bóc tách và chấm điểm hàng loạt file PDF CV bằng Gemini 2.5 Flash
    /// </summary>
    [HttpPost("batch-analyze")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> BatchAnalyzeCvs(
        [FromForm] List<IFormFile> files,
        [FromForm] Guid jobId,
        [FromForm] int skillsWeight = 40,
        [FromForm] int experienceWeight = 30,
        [FromForm] int healthShiftWeight = 30)
    {
        if (files == null || !files.Any())
        {
            return BadRequest(new { success = false, message = "Vui lòng chọn ít nhất 1 file CV định dạng PDF." });
        }

        try
        {
            _logger.LogInformation("Bắt đầu AI Bulk Screening cho {Count} CVs. JobId: {JobId}", files.Count, jobId);

            var results = await _aiScreeningService.ScreenBulkCvsAsync(
                files,
                jobId,
                skillsWeight,
                experienceWeight,
                healthShiftWeight);

            return Ok(new
            {
                success = true,
                message = $"Đã phân tích và xếp hạng thành công {results.Count} hồ sơ ứng viên.",
                data = results,
                meta = new
                {
                    totalProcessed = results.Count,
                    topScore = results.FirstOrDefault()?.MatchingScore ?? 0,
                    averageScore = results.Any() ? (int)results.Average(r => r.MatchingScore) : 0,
                    qualifiedCount = results.Count(r => r.IsQualified)
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi trong quá trình AI Bulk Screening");
            return StatusCode(500, new { success = false, message = "Có lỗi xảy ra khi Gemini phân tích CV: " + ex.Message });
        }
    }

    /// <summary>
    /// Chuyển ứng viên đã sàng lọc sang Quản đốc xưởng duyệt chuyên môn
    /// </summary>
    [HttpPost("forward-to-manager")]
    public async Task<IActionResult> ForwardToManager([FromBody] ForwardToManagerRequestDto request)
    {
        if (request == null || !request.CandidateIds.Any())
        {
            return BadRequest(new { success = false, message = "Danh sách ứng viên không được để trống." });
        }

        try
        {
            var success = await _aiScreeningService.ForwardCandidatesToManagerAsync(request);
            return Ok(new
            {
                success = true,
                message = $"Đã chuyển thành công {request.CandidateIds.Count} hồ sơ tới Quản đốc xưởng ({request.DepartmentCode}) phê duyệt phỏng vấn."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi chuyển hồ sơ sang Quản đốc");
            return StatusCode(500, new { success = false, message = "Không thể chuyển hồ sơ: " + ex.Message });
        }
    }
}
