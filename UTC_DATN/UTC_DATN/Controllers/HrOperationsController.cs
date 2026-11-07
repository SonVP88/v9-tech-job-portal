using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using UTC_DATN.Data;
using UTC_DATN.Services.Interfaces;

namespace UTC_DATN.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class HrOperationsController : ControllerBase
{
    private readonly IHrOperationsBusinessService _hrOperationsService;
    private readonly UTC_DATNContext _context;
    private readonly ILogger<HrOperationsController> _logger;

    public HrOperationsController(
        IHrOperationsBusinessService hrOperationsService,
        UTC_DATNContext context,
        ILogger<HrOperationsController> logger)
    {
        _hrOperationsService = hrOperationsService;
        _context = context;
        _logger = logger;
    }

    private Guid? GetCurrentUserId()
    {
        var val = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(val, out var id) ? id : null;
    }

    /// <summary>
    /// Logic 1: Nhận diện Nhân viên cũ tái ứng tuyển (Ex-Employee Rehire)
    /// </summary>
    [HttpPost("applications/{applicationId}/check-rehire")]
    [Authorize(Roles = "HR, ADMIN, HR_TEAM")]
    public async Task<IActionResult> CheckExEmployeeRehire(Guid applicationId, [FromBody] CheckRehireRequest? request)
    {
        var app = await _context.Applications.FirstOrDefaultAsync(a => a.ApplicationId == applicationId);
        if (app == null)
        {
            return NotFound(new { success = false, message = "Không tìm thấy hồ sơ ứng viên." });
        }

        var result = await _hrOperationsService.CheckExEmployeeRehireAsync(
            app, request?.Phone, request?.CitizenId);

        if (result.IsRehire)
        {
            await _context.SaveChangesAsync();
        }

        var currentScore = await _context.ApplicationAiScores
            .Where(s => s.ApplicationId == applicationId)
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => (decimal?)s.MatchingScore)
            .FirstOrDefaultAsync();

        return Ok(new
        {
            success = true,
            isRehire = result.IsRehire,
            employeeCode = result.EmployeeCode,
            message = result.Message,
            newScore = currentScore
        });
    }

    /// <summary>
    /// Logic 2: Chia tách Ca phỏng vấn tự động (Max 50 người/ca) & Chống trùng lịch người PV
    /// </summary>
    [HttpPost("schedule-batches")]
    [Authorize(Roles = "HR, ADMIN, HR_TEAM")]
    public async Task<IActionResult> ScheduleBatchInterviews([FromBody] ScheduleBatchInterviewRequest request)
    {
        if (request == null || request.ApplicationIds == null || request.ApplicationIds.Count == 0)
        {
            return BadRequest(new { success = false, message = "Vui lòng chọn ít nhất một hồ sơ ứng viên để xếp ca." });
        }

        var result = await _hrOperationsService.ScheduleBatchInterviewsAsync(request, GetCurrentUserId());
        if (!result.Success)
        {
            return BadRequest(new { success = false, message = result.Message });
        }

        return Ok(result);
    }

    /// <summary>
    /// Logic 3: Giới hạn Dời lịch Phỏng vấn tối đa 2 lần. Lần thứ 3 chuyển Status = 'Rejected_Exceed_Reschedule'.
    /// </summary>
    [HttpPost("applications/{applicationId}/reschedule")]
    [Authorize(Roles = "HR, ADMIN, HR_TEAM, INTERVIEWER")]
    public async Task<IActionResult> RescheduleInterview(Guid applicationId, [FromBody] RescheduleInterviewRequest request)
    {
        if (request == null)
        {
            return BadRequest(new { success = false, message = "Dữ liệu yêu cầu dời lịch không hợp lệ." });
        }

        var result = await _hrOperationsService.RescheduleInterviewAsync(
            applicationId, request.NewDate, request.ShiftSlot ?? "MORNING_08_11", request.Reason, GetCurrentUserId());

        return Ok(new
        {
            success = result.Success,
            isRejected = result.IsRejected,
            currentRescheduleCount = result.CurrentCount,
            message = result.Message
        });
    }

    /// <summary>
    /// Logic 4: Quét và Tự động Hết hạn Thư mời nhận việc (Offer Expiration) sau 72h
    /// </summary>
    [HttpPost("expire-offers-scan")]
    [Authorize(Roles = "HR, ADMIN, HR_TEAM")]
    public async Task<IActionResult> CheckAndExpireOffers()
    {
        var result = await _hrOperationsService.CheckAndExpireOffersAsync();
        return Ok(new
        {
            success = true,
            expiredCount = result.ExpiredCount,
            expiredApplicationIds = result.ExpiredApplicationIds,
            message = $"Đã quét và xử lý hết hạn {result.ExpiredCount} thư mời nhận việc quá 72h."
        });
    }

    /// <summary>
    /// Logic 5: Tự động Đóng chiến dịch & Bảo lưu hồ sơ vào Kho ứng viên tiềm năng (Talent Pool)
    /// </summary>
    [HttpPost("jobs/{jobId}/close-talent-pool")]
    [Authorize(Roles = "HR, ADMIN, HR_TEAM")]
    public async Task<IActionResult> AutoCloseCampaignAndMoveToTalentPool(Guid jobId)
    {
        var result = await _hrOperationsService.AutoCloseCampaignAndMoveToTalentPoolAsync(jobId, GetCurrentUserId());
        if (!result.Success)
        {
            return BadRequest(new { success = false, message = result.Message });
        }

        return Ok(new
        {
            success = true,
            movedCount = result.MovedToTalentPoolCount,
            message = result.Message
        });
    }

    /// <summary>
    /// Logic 6: Đánh giá thử việc sau 30 ngày (Probation 30-Day Feedback) & Đo lường tỷ lệ thành công
    /// </summary>
    [HttpPost("applications/{applicationId}/probation-feedback")]
    [Authorize(Roles = "HR, ADMIN, HR_TEAM")]
    public async Task<IActionResult> UpdateProbationFeedback(Guid applicationId, [FromBody] ProbationFeedbackRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.ProbationStatus))
        {
            return BadRequest(new { success = false, message = "Vui lòng chọn kết quả đánh giá thử việc 30 ngày." });
        }

        var result = await _hrOperationsService.UpdateProbationFeedbackAsync(applicationId, request, GetCurrentUserId());
        if (!result.Success)
        {
            return BadRequest(new { success = false, message = result.Message });
        }

        return Ok(new
        {
            success = true,
            message = result.Message,
            accuracyRate = result.AccuracyRate
        });
    }

    /// <summary>
    /// Lấy danh sách các ca phỏng vấn tập trung đang hoạt động
    /// </summary>
    [HttpGet("batches")]
    [Authorize(Roles = "HR, ADMIN, HR_TEAM, INTERVIEWER")]
    public async Task<IActionResult> GetBatches([FromQuery] Guid? jobId)
    {
        var batches = await _hrOperationsService.GetActiveInterviewBatchesAsync(jobId);
        return Ok(batches);
    }
}

public class CheckRehireRequest
{
    public string? Phone { get; set; }
    public string? CitizenId { get; set; }
}

public class RescheduleInterviewRequest
{
    public DateTime NewDate { get; set; } = DateTime.UtcNow.AddDays(1);
    public string ShiftSlot { get; set; } = "MORNING_08_11";
    public string? Reason { get; set; }
}
