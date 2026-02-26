using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using UTC_DATN.DTOs.Application;
using UTC_DATN.DTOs.Interview;
using UTC_DATN.Services.Interfaces;

namespace UTC_DATN.Controllers;

[ApiController]
[Route("api/applications")]
public class ApplicationController : ControllerBase
{
    private readonly IApplicationService _applicationService;
    private readonly IInterviewService _interviewService;
    private readonly ILogger<ApplicationController> _logger;

    public ApplicationController(
        IApplicationService applicationService,
        IInterviewService interviewService,
        ILogger<ApplicationController> logger)
    {
        _applicationService = applicationService;
        _interviewService = interviewService;
        _logger = logger;
    }

    /// <summary>
    /// API nộp hồ sơ ứng tuyển
    /// </summary>
    /// <param name="request">Thông tin ứng tuyển bao gồm file CV</param>
    /// <returns>Kết quả nộp hồ sơ</returns>
    [HttpPost("apply")]
    public async Task<IActionResult> ApplyJob([FromForm] ApplyJobRequest request)
    {
        try
        {
            // Validate ModelState
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                _logger.LogWarning("Validation failed: {Errors}", string.Join(", ", errors));
                
                return BadRequest(new
                {
                    success = false,
                    message = "Dữ liệu không hợp lệ",
                    errors = errors
                });
            }

            // === LẤY UserId từ JWT Token (nếu user đã đăng nhập) ===
            Guid? userId = null;
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            
            if (!string.IsNullOrEmpty(userIdClaim) && Guid.TryParse(userIdClaim, out var parsedUserId))
            {
                userId = parsedUserId;
                _logger.LogInformation(" User đã đăng nhập - UserId: {UserId}", userId);
            }
            else
            {
                _logger.LogInformation("ℹ️ User chưa đăng nhập - Apply dạng Guest");
            }

            // Gọi service với userId
            var result = await _applicationService.ApplyJobAsync(request, userId);

            if (result)
            {
                _logger.LogInformation("Nộp hồ sơ thành công cho JobId: {JobId}, Email: {Email}, UserId: {UserId}", 
                    request.JobId, request.Email, userId?.ToString() ?? "NULL");

                return Ok(new
                {
                    success = true,
                    message = "Nộp hồ sơ thành công! Chúng tôi sẽ liên hệ với bạn sớm nhất."
                });
            }
            else
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Có lỗi xảy ra khi nộp hồ sơ. Vui lòng thử lại sau."
                });
            }
        }
        catch (ArgumentException ex)
        {
            // Lỗi validation hoặc business logic
            _logger.LogWarning(ex, "Validation error khi nộp hồ sơ");
            return BadRequest(new
            {
                success = false,
                message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            // Lỗi duplicate application
            _logger.LogWarning(ex, "Conflict khi nộp hồ sơ");
            return Conflict(new
            {
                success = false,
                message = ex.Message
            });
        }
        catch (Exception ex)
        {
            // Lỗi hệ thống
            _logger.LogError(ex, "Lỗi hệ thống khi nộp hồ sơ");
            return StatusCode(500, new
            {
                success = false,
                message = "Có lỗi hệ thống xảy ra. Vui lòng thử lại sau."
            });
        }
    }

    /// <summary>
    /// Lấy danh sách ứng viên của một Job (Dành cho HR/Admin)
    /// </summary>
    /// <param name="jobId">ID của công việc</param>
    /// <returns>Danh sách ứng viên kèm điểm số AI</returns>
    [HttpGet("~/api/jobs/{jobId}/applications")]
    [Authorize(Roles = "HR, ADMIN")]
    public async Task<IActionResult> GetApplicationsByJobId(Guid jobId)
    {
        try
        {
            var applications = await _applicationService.GetApplicationsByJobIdAsync(jobId);
            return Ok(new
            {
                success = true,
                data = applications
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy danh sách ứng viên cho JobId: {JobId}", jobId);
            return StatusCode(500, new
            {
                success = false,
                message = "Có lỗi xảy ra khi lấy danh sách ứng viên."
            });
        }
    }

    /// <summary>
    /// API cập nhật trạng thái hồ sơ ứng tuyển (Dành cho HR/Admin)
    /// </summary>
    [HttpPut("{id}/status")]
    [Authorize(Roles = "HR, ADMIN")]
    public async Task<IActionResult> UpdateApplicationStatus(Guid id, [FromQuery] string status)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(status))
            {
                return BadRequest(new { success = false, message = "Trạng thái không được để trống" });
            }

            var result = await _applicationService.UpdateStatusAsync(id, status);

            if (result != null && result.Success)
            {
                return Ok(new { 
                    success = true, 
                    message = "Cập nhật trạng thái thành công",
                    data = result 
                });
            }
            else
            {
                return NotFound(new { success = false, message = "Không tìm thấy hồ sơ hoặc cập nhật thất bại" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi cập nhật trạng thái hồ sơ ID: {Id}", id);
            return StatusCode(500, new { success = false, message = "Có lỗi xảy ra khi cập nhật trạng thái." });
        }
    }

    /// <summary>
    /// API cho ứng viên phản hồi Offer (Đồng ý / Từ chối)
    /// Chỉ CANDIDATE mới được gọi. Chỉ cho phép đổi Offer_Sent → HIRED hoặc REJECTED.
    /// </summary>
    [HttpPut("{id}/respond-offer")]
    [Authorize]
    public async Task<IActionResult> RespondToOffer(Guid id, [FromQuery] string accept)
    {
        try
        {
            // Chỉ nhận "true" (đồng ý) hoặc "false" (từ chối)
            if (!bool.TryParse(accept, out var isAccepted))
                return BadRequest(new { success = false, message = "Tham số 'accept' phải là true hoặc false." });

            var newStatus = isAccepted ? "HIRED" : "REJECTED";
            var result = await _applicationService.UpdateStatusAsync(id, newStatus);

            if (result != null && result.Success)
            {
                return Ok(new
                {
                    success = true,
                    message = isAccepted ? "Bạn đã chấp nhận Offer. Chúc mừng!" : "Bạn đã từ chối Offer.",
                    data = result
                });
            }
            return NotFound(new { success = false, message = "Không tìm thấy hồ sơ hoặc cập nhật thất bại." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi ứng viên phản hồi Offer ID: {Id}", id);
            return StatusCode(500, new { success = false, message = "Có lỗi xảy ra." });
        }
    }


    [HttpGet("my-applications")]
    [Authorize]
    public async Task<IActionResult> GetMyApplications()
    {
        try
        {
            _logger.LogInformation("=== GetMyApplications Started ===");
            
            // Log all claims
            var allClaims = User.Claims.Select(c => $"{c.Type}: {c.Value}").ToList();
            _logger.LogInformation("All JWT Claims: {Claims}", string.Join(", ", allClaims));
            
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            _logger.LogInformation("UserId Claim Value: {UserIdClaim}", userIdClaim ?? "NULL");
            
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                _logger.LogWarning(" UserId claim is missing or invalid!");
                return Unauthorized(new { success = false, message = "Vui lòng đăng nhập để xem hồ sơ." });
            }

            _logger.LogInformation(" Valid UserId: {UserId}", userId);
            
            var applications = await _applicationService.GetMyApplicationsAsync(userId);
            
            _logger.LogInformation("Service returned {Count} applications", applications.Count);
            
            return Ok(new
            {
                success = true,
                data = applications
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, " Exception in GetMyApplications");
            return StatusCode(500, new
            {
                success = false,
                message = "Có lỗi xảy ra khi lấy danh sách hồ sơ."
            });
        }
    }

    /// <summary>
    /// Lấy toàn bộ danh sách hồ sơ ứng tuyển (Dành cho HR/Admin)
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "HR, ADMIN")]
    public async Task<IActionResult> GetAllApplications()
    {
        try
        {
            var applications = await _applicationService.GetAllApplicationsAsync();
            return Ok(new
            {
                success = true,
                data = applications
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy danh sách toàn bộ hồ sơ ứng tuyển");
            return StatusCode(500, new
            {
                success = false,
                message = "Có lỗi xảy ra khi lấy danh sách ứng viên."
            });
        }
    }

    /// <summary>
    /// API lên lịch phỏng vấn cho một Application (Dành cho HR/Admin)
    /// </summary>
    [HttpPost("{applicationId}/schedule-interview")]
    [Authorize(Roles = "HR, ADMIN")]
    public async Task<IActionResult> ScheduleInterview(Guid applicationId, [FromBody] ScheduleInterviewDto dto)
    {
        try
        {
            // Override applicationId from route to DTO
            dto.ApplicationId = applicationId;

            // Validate ModelState
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                _logger.LogWarning("Validation failed for schedule interview: {Errors}", string.Join(", ", errors));
                return BadRequest(new
                {
                    success = false,
                    message = "Dữ liệu không hợp lệ",
                    errors = errors
                });
            }

            // Lấy createdBy từ JWT claims
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var createdBy))
            {
                return Unauthorized(new { success = false, message = "Vui lòng đăng nhập để thực hiện hành động này." });
            }

            // Gọi service
            var interviewId = await _interviewService.ScheduleInterviewAsync(dto, createdBy);

            _logger.LogInformation("Interview scheduled successfully with ID: {InterviewId} for ApplicationId: {ApplicationId}", 
                interviewId, applicationId);

            return Ok(new
            {
                success = true,
                message = "Lên lịch phỏng vấn thành công",
                data = new { interviewId }
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation when scheduling interview");
            return BadRequest(new
            {
                success = false,
                message = ex.Message
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Argument validation error when scheduling interview");
            return BadRequest(new
            {
                success = false,
                message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scheduling interview for ApplicationId: {ApplicationId}", applicationId);
            return StatusCode(500, new
            {
                success = false,
                message = "Có lỗi xảy ra khi lên lịch phỏng vấn."
            });
        }
    }
}
