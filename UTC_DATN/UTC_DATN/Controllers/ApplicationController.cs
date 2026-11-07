using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using UTC_DATN.Data;
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
    private readonly UTC_DATNContext _context;
    private readonly IConfiguration _configuration;
    private readonly ICampaignQrService _campaignQrService;

    public ApplicationController(
        IApplicationService applicationService,
        IInterviewService interviewService,
        ILogger<ApplicationController> logger,
        UTC_DATNContext context,
        IConfiguration configuration,
        ICampaignQrService campaignQrService)
    {
        _applicationService = applicationService;
        _interviewService = interviewService;
        _logger = logger;
        _context = context;
        _configuration = configuration;
        _campaignQrService = campaignQrService;
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
    [Authorize(Roles = "HR, ADMIN, INTERVIEWER")]
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

            Guid? actorUserId = null;
            var actorClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(actorClaim) && Guid.TryParse(actorClaim, out var parsedActor))
            {
                actorUserId = parsedActor;
            }

            var result = await _applicationService.UpdateStatusAsync(id, status, actorUserId: actorUserId);

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
    /// Chỉ CANDIDATE mới được gọi. Chỉ cho phép đổi Offer_Sent → OFFER_ACCEPTED hoặc REJECTED.
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

            var newStatus = isAccepted ? "OFFER_ACCEPTED" : "REJECTED";
            Guid? actorUserId = null;
            var actorClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(actorClaim) && Guid.TryParse(actorClaim, out var parsedActor))
            {
                actorUserId = parsedActor;
            }
            // isHrAction = false -> candidate tự phản hồi -> trigger notification đúng
            var result = await _applicationService.UpdateStatusAsync(id, newStatus, isHrAction: false, actorUserId: actorUserId);


            if (result != null && result.Success)
            {
                return Ok(new
                {
                    success = true,
                    message = isAccepted ? "Bạn đã chấp nhận Offer. HR sẽ xác nhận bước nhận việc tiếp theo." : "Bạn đã từ chối Offer.",
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
    [Authorize(Roles = "HR, ADMIN, INTERVIEWER")]
    public async Task<IActionResult> GetAllApplications([FromQuery] IndustrialApplicationFilterQuery? filter = null)
    {
        try
        {
            var applications = await GetIndustrialFilteredApplicationsAsync(filter);
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
    private async Task<List<ApplicationDto>> GetIndustrialFilteredApplicationsAsync(IndustrialApplicationFilterQuery? filter)
    {
        var allApps = await _applicationService.GetAllApplicationsAsync();

        // Data Isolation: SuperAdmin/ADMIN và HR_TEAM/HR thấy tất cả; DepartmentManager/INTERVIEWER chỉ thấy hồ sơ thuộc DepartmentId của xưởng mình
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var userRoles = User.FindAll(System.Security.Claims.ClaimTypes.Role).Select(r => r.Value.ToUpperInvariant()).ToList();
        var isFullAccess = userRoles.Contains("ADMIN") || userRoles.Contains("SUPERADMIN") || userRoles.Contains("HR") || userRoles.Contains("HR_TEAM");

        if (!isFullAccess && Guid.TryParse(userIdClaim, out var currentUserId))
        {
            var currentUser = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == currentUserId);
            if (currentUser?.DepartmentId != null)
            {
                var allowedJobIds = await _context.Jobs
                    .AsNoTracking()
                    .Where(j => j.DepartmentId == currentUser.DepartmentId && !j.IsDeleted)
                    .Select(j => j.JobId)
                    .ToListAsync();
                var allowedSet = new HashSet<Guid>(allowedJobIds);
                allApps = allApps.Where(a => allowedSet.Contains(a.JobId)).ToList();
            }
        }

        if (allApps.Count > 0)
        {
            var ids = allApps.Select(x => x.ApplicationId).ToList();
            var enrichMeta = await _context.Applications
                .AsNoTracking()
                .Include(a => a.Job)
                    .ThenInclude(j => j.DepartmentNavigation)
                .Include(a => a.Candidate)
                .Where(a => ids.Contains(a.ApplicationId))
                .Select(a => new
                {
                    a.ApplicationId,
                    a.Source,
                    a.ApplicationSource,
                    a.ScoringMethod,
                    a.CvFileUrl,
                    a.RawResumeText,
                    a.OralInterviewChecklistJson,
                    a.ResumeDocumentId,
                    ResumeFileUrl = a.ResumeDocument != null && a.ResumeDocument.File != null ? (a.ResumeDocument.File.Url ?? a.ResumeDocument.File.LocalPath) : null,
                    CandidateSummary = a.Candidate != null ? a.Candidate.Summary : null,
                    JobDepartmentId = a.Job.DepartmentId,
                    JobDepartmentName = a.Job.DepartmentNavigation != null ? a.Job.DepartmentNavigation.DepartmentName : a.Job.Department,
                    JobDepartmentCode = a.Job.DepartmentNavigation != null ? a.Job.DepartmentNavigation.DepartmentCode : "PROD_LINE",
                    JobRecruitmentTrack = a.Job.RecruitmentTrack
                })
                .ToDictionaryAsync(x => x.ApplicationId);

            foreach (var app in allApps)
            {
                if (!enrichMeta.TryGetValue(app.ApplicationId, out var m)) continue;
                var resolvedSource = !string.IsNullOrWhiteSpace(m.ApplicationSource)
                    ? m.ApplicationSource
                    : (m.Source != null && m.Source.Contains("GOOGLE") ? "GOOGLE_FORM_QR"
                        : (m.Source == "WALK_IN" ? "WALK_IN" : (!string.IsNullOrWhiteSpace(app.CvUrl) || m.ResumeDocumentId != null ? "PDF_UPLOAD" : "GOOGLE_FORM_QR")));

                app.ApplicationSource = resolvedSource;
                app.RecruitmentTrack = !string.IsNullOrWhiteSpace(m.JobRecruitmentTrack)
                    ? m.JobRecruitmentTrack
                    : (resolvedSource == "PDF_UPLOAD" ? "PROFESSIONAL" : "MASS_LABOR");
                app.ScoringMethod = !string.IsNullOrWhiteSpace(m.ScoringMethod)
                    ? m.ScoringMethod
                    : (resolvedSource == "PDF_UPLOAD" ? "GEMINI_AI" : "RULE_ENGINE");
                app.CvFileUrl = !string.IsNullOrWhiteSpace(m.CvFileUrl) ? m.CvFileUrl : (!string.IsNullOrWhiteSpace(app.CvUrl) ? app.CvUrl : m.ResumeFileUrl);
                app.RawResumeText = !string.IsNullOrWhiteSpace(m.RawResumeText) ? m.RawResumeText : m.CandidateSummary;
                app.OralInterviewChecklistJson = m.OralInterviewChecklistJson;
                app.DepartmentId = m.JobDepartmentId;
                app.DepartmentName = m.JobDepartmentName;
                app.DepartmentCode = m.JobDepartmentCode;
            }
        }

        if (filter == null) return allApps;

        var query = allApps.AsEnumerable();

        if (filter.JobPostingId.HasValue)
            query = query.Where(a => a.JobId == filter.JobPostingId.Value);

        if (filter.MinAiScore.HasValue)
            query = query.Where(a => (a.MatchScore ?? 0) >= filter.MinAiScore.Value);

        if (!string.IsNullOrWhiteSpace(filter.Status))
            query = query.Where(a => string.Equals(a.Status, filter.Status.Trim(), StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(filter.Keyword))
        {
            var kw = filter.Keyword.Trim().ToLowerInvariant();
            query = query.Where(a =>
                (a.CandidateName?.ToLowerInvariant().Contains(kw) ?? false) ||
                (a.Phone?.ToLowerInvariant().Contains(kw) ?? false) ||
                (a.Email?.ToLowerInvariant().Contains(kw) ?? false) ||
                (a.JobTitle?.ToLowerInvariant().Contains(kw) ?? false));
        }

        if (filter.DepartmentId.HasValue || !string.IsNullOrWhiteSpace(filter.RecruitmentTrack) || !string.IsNullOrWhiteSpace(filter.ApplicationSource))
        {
            var appIds = query.Select(a => a.ApplicationId).ToList();
            var dbMeta = await _context.Applications
                .AsNoTracking()
                .Include(a => a.Job)
                .Where(a => appIds.Contains(a.ApplicationId))
                .Select(a => new
                {
                    a.ApplicationId,
                    a.Source,
                    a.ApplicationSource,
                    a.CvFileUrl,
                    a.ResumeDocumentId,
                    JobDepartmentId = a.Job.DepartmentId,
                    JobRecruitmentTrack = a.Job.RecruitmentTrack,
                    JobTitle = a.Job.Title
                })
                .ToDictionaryAsync(x => x.ApplicationId);

            query = query.Where(a =>
            {
                if (!dbMeta.TryGetValue(a.ApplicationId, out var meta)) return true;

                if (filter.DepartmentId.HasValue && meta.JobDepartmentId != filter.DepartmentId.Value)
                    return false;

                var resolvedSource = !string.IsNullOrWhiteSpace(meta.ApplicationSource)
                    ? meta.ApplicationSource
                    : (meta.Source != null && meta.Source.Contains("GOOGLE") ? "GOOGLE_FORM_QR"
                        : (meta.Source == "WALK_IN" ? "WALK_IN" : (!string.IsNullOrWhiteSpace(a.CvUrl) || meta.ResumeDocumentId != null ? "PDF_UPLOAD" : "GOOGLE_FORM_QR")));

                if (!string.IsNullOrWhiteSpace(filter.ApplicationSource) &&
                    !string.Equals(resolvedSource, filter.ApplicationSource.Trim(), StringComparison.OrdinalIgnoreCase))
                    return false;

                var resolvedTrack = !string.IsNullOrWhiteSpace(meta.JobRecruitmentTrack)
                    ? meta.JobRecruitmentTrack
                    : (resolvedSource == "PDF_UPLOAD" ? "PROFESSIONAL" : "MASS_LABOR");

                if (!string.IsNullOrWhiteSpace(filter.RecruitmentTrack) &&
                    !string.Equals(resolvedTrack, filter.RecruitmentTrack.Trim(), StringComparison.OrdinalIgnoreCase))
                    return false;

                return true;
            });
        }

        return query.ToList();
    }

    /// <summary>
    /// POST /api/applications/bulk-action
    /// Duyệt / chuyển Quản đốc / xếp lịch phỏng vấn / từ chối hàng loạt ứng viên bằng 1 click
    /// </summary>
    [HttpPost("bulk-action")]
    [Authorize]
    public async Task<IActionResult> BulkAction([FromBody] BulkApplicationActionRequest request)
    {
        if (request == null || request.ApplicationIds == null || request.ApplicationIds.Count == 0)
        {
            return BadRequest(new { success = false, message = "Vui lòng chọn ít nhất 1 hồ sơ ứng viên." });
        }

        try
        {
            Guid? actorUserId = null;
            var actorClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(actorClaim) && Guid.TryParse(actorClaim, out var parsedActor))
                actorUserId = parsedActor;

            // Chuẩn hóa TargetStatus sang mã hệ thống
            var normalizedStatus = request.TargetStatus.Trim() switch
            {
                "SentToManager" => "SCREENING",
                "ScheduledInterview" => "INTERVIEW",
                "InterviewPassed" => "PASSED",
                "Rejected" => "REJECTED",
                _ => request.TargetStatus.Trim().ToUpperInvariant()
            };

            var targetStage = await _context.PipelineStages
                .OrderBy(s => s.SortOrder)
                .FirstOrDefaultAsync(s => s.Code == normalizedStatus || s.Name.Contains(request.TargetStatus));

            var apps = await _context.Applications
                .Where(a => request.ApplicationIds.Contains(a.ApplicationId))
                .ToListAsync();

            var now = DateTime.UtcNow;
            foreach (var app in apps)
            {
                app.Status = normalizedStatus;
                app.LastStageChangedAt = now;
                if (targetStage != null)
                {
                    app.CurrentStageId = targetStage.StageId;
                }

                if (!string.IsNullOrWhiteSpace(request.Note))
                {
                    _context.ApplicationNotes.Add(new Entities.ApplicationNote
                    {
                        NoteId = Guid.NewGuid(),
                        ApplicationId = app.ApplicationId,
                        Note = $"[Thao tác hàng loạt - {request.TargetStatus}]: {request.Note}",
                        CreatedBy = actorUserId,
                        CreatedAt = now
                    });
                }
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = $"Đã cập nhật thành công {apps.Count} hồ sơ sang trạng thái '{request.TargetStatus}'.",
                data = new
                {
                    updatedCount = apps.Count,
                    targetStatus = request.TargetStatus,
                    normalizedStatus
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi thực hiện bulk-action cho {Count} hồ sơ", request.ApplicationIds.Count);
            return StatusCode(500, new { success = false, message = "Có lỗi xảy ra khi xử lý hàng loạt.", detail = ex.Message });
        }
    }

    /// <summary>
    /// POST /api/applications/quick-walkin-eval
    /// Dành riêng cho phỏng vấn miệng nhanh công nhân xưởng (Fast-Track Walk-in Oral Interview)
    /// Hỗ trợ tạo mới nhanh tại cổng nhà máy hoặc chấm trực tiếp ứng viên từ QR Form
    /// </summary>
    [HttpPost("quick-walkin-eval")]
    [Authorize]
    public async Task<IActionResult> QuickWalkInEvaluate([FromBody] QuickWalkInEvalRequest request)
    {
        if (request == null)
            return BadRequest(new { success = false, message = "Dữ liệu đánh giá không hợp lệ." });

        try
        {
            var now = DateTime.UtcNow;
            var checklistJson = System.Text.Json.JsonSerializer.Serialize(request.Checklist ?? new OralInterviewChecklistDto());
            var isPassed = !string.Equals(request.TargetStatus, "Rejected", StringComparison.OrdinalIgnoreCase)
                           && !string.Equals(request.TargetStatus, "REJECTED", StringComparison.OrdinalIgnoreCase);
            var finalStatus = isPassed ? "PASSED" : "REJECTED";

            var passedStage = await _context.PipelineStages
                .OrderBy(s => s.SortOrder)
                .FirstOrDefaultAsync(s => isPassed
                    ? (s.Code == "PASSED" || s.Code == "OFFER" || s.Code == "INTERVIEW")
                    : s.Code == "REJECTED")
                ?? await _context.PipelineStages.OrderBy(s => s.SortOrder).FirstOrDefaultAsync();

            Entities.Application? app = null;

            if (request.ApplicationId.HasValue && request.ApplicationId.Value != Guid.Empty)
            {
                app = await _context.Applications
                    .Include(a => a.Candidate)
                    .FirstOrDefaultAsync(a => a.ApplicationId == request.ApplicationId.Value);
            }

            // Nếu ứng viên đến trực tiếp tại cổng nhà máy (Walk-in) chưa có hồ sơ -> Tạo mới nhanh
            if (app == null)
            {
                if (string.IsNullOrWhiteSpace(request.FullName) || string.IsNullOrWhiteSpace(request.Phone))
                {
                    return BadRequest(new { success = false, message = "Vui lòng nhập Họ tên và Số điện thoại cho ứng viên Walk-in tại cổng." });
                }

                var targetJob = request.JobPostingId.HasValue
                    ? await _context.Jobs.FirstOrDefaultAsync(j => j.JobId == request.JobPostingId.Value && !j.IsDeleted)
                    : await _context.Jobs.FirstOrDefaultAsync(j => !j.IsDeleted && j.Status == "OPEN");

                if (targetJob == null)
                {
                    targetJob = await _context.Jobs.FirstOrDefaultAsync(j => !j.IsDeleted);
                }

                if (targetJob == null)
                {
                    return BadRequest(new { success = false, message = "Chưa có chiến dịch tuyển dụng nào trong hệ thống." });
                }

                var cleanPhone = request.Phone.Trim();
                var candidate = await _context.Candidates.FirstOrDefaultAsync(c => c.Phone == cleanPhone);
                var rawSummary = $"Năm sinh: {request.YearOfBirth ?? "N/A"} | CCCD: {request.Cccd ?? "N/A"} | Quê quán: {request.Hometown ?? "N/A"} | Trình độ: {request.EducationLevel ?? "THPT"} | Kinh nghiệm: {request.ExperienceSummary ?? "Lao động phổ thông"}";

                if (candidate == null)
                {
                    candidate = new Entities.Candidate
                    {
                        CandidateId = Guid.NewGuid(),
                        FullName = request.FullName.Trim(),
                        Phone = cleanPhone,
                        Email = $"{cleanPhone}@walkin-factory.local",
                        Location = request.Hometown?.Trim() ?? string.Empty,
                        Headline = $"Công nhân Walk-in tại cổng - {request.FullName.Trim()}",
                        Summary = rawSummary,
                        Source = "WALK_IN",
                        CreatedAt = now
                    };
                    _context.Candidates.Add(candidate);
                }

                app = new Entities.Application
                {
                    ApplicationId = Guid.NewGuid(),
                    JobId = targetJob.JobId,
                    CandidateId = candidate.CandidateId,
                    CurrentStageId = passedStage?.StageId ?? Guid.Empty,
                    Source = "WALK_IN",
                    ApplicationSource = "WALK_IN",
                    ScoringMethod = "RULE_ENGINE",
                    CvFileUrl = null,
                    RawResumeText = rawSummary,
                    OralInterviewChecklistJson = checklistJson,
                    Status = finalStatus,
                    AppliedAt = now,
                    LastStageChangedAt = now,
                    ContactName = candidate.FullName,
                    ContactPhone = candidate.Phone,
                    ContactEmail = candidate.Email
                };
                _context.Applications.Add(app);
            }
            else
            {
                app.OralInterviewChecklistJson = checklistJson;
                app.Status = finalStatus;
                app.LastStageChangedAt = now;
                if (passedStage != null)
                {
                    app.CurrentStageId = passedStage.StageId;
                }
            }

            // Tính điểm Rule Engine dựa trên Checklist phỏng vấn miệng
            var cl = request.Checklist ?? new OralInterviewChecklistDto();
            int passedChecks = (cl.HandEyePass ? 25 : 0)
                             + (cl.VisionPass ? 25 : 0)
                             + (cl.Shift12hConfirmed ? 30 : 0)
                             + (cl.AttitudePass ? 20 : 0);

            var existingScore = await _context.ApplicationAiScores.FirstOrDefaultAsync(s => s.ApplicationId == app.ApplicationId);
            if (existingScore == null)
            {
                _context.ApplicationAiScores.Add(new Entities.ApplicationAiScore
                {
                    AiScoreId = Guid.NewGuid(),
                    ApplicationId = app.ApplicationId,
                    MatchingScore = passedChecks,
                    Model = "RULE_ENGINE_ORAL",
                    BreakdownJson = checklistJson,
                    MatchedSkillsJson = checklistJson,
                    CreatedAt = now
                });
            }
            else
            {
                existingScore.MatchingScore = passedChecks;
                existingScore.BreakdownJson = checklistJson;
            }

            _context.ApplicationNotes.Add(new Entities.ApplicationNote
            {
                NoteId = Guid.NewGuid(),
                ApplicationId = app.ApplicationId,
                Note = $"⚡ [Phỏng vấn miệng nhanh - {(isPassed ? "ĐẠT NGAY -> ONBOARDING" : "LOẠI")}]: Tay chân={(cl.HandEyePass ? "✅" : "❌")}, Thị lực={(cl.VisionPass ? "✅" : "❌")}, Xoay ca 12h={(cl.Shift12hConfirmed ? "✅" : "❌")}, Thái độ={(cl.AttitudePass ? "✅" : "❌")}. Ghi chú: {cl.QuickNote}",
                CreatedAt = now
            });

            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = isPassed
                    ? "Đã chấm ĐẠT phỏng vấn miệng và chuyển ứng viên sang Tiếp Nhận & Onboarding!"
                    : "Đã ghi nhận kết quả LOẠI phỏng vấn miệng.",
                data = new
                {
                    applicationId = app.ApplicationId,
                    candidateId = app.CandidateId,
                    status = app.Status,
                    score = passedChecks,
                    checklist = request.Checklist
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi xử lý quick-walkin-eval");
            return StatusCode(500, new { success = false, message = "Lỗi khi lưu đánh giá phỏng vấn miệng.", detail = ex.Message });
        }
    }

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

    /// <summary>
    /// API ghi nhận lượt xem CV (Dành cho HR/Admin)
    /// </summary>
    [HttpPost("{id}/track-view")]
    [Authorize(Roles = "HR, ADMIN, INTERVIEWER")]
    public async Task<IActionResult> TrackView(Guid id)
    {
        try
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var viewerId))
                return Unauthorized(new { success = false, message = "Vui lòng đăng nhập." });

            var result = await _applicationService.TrackViewAsync(id, viewerId);
            if (result)
                return Ok(new { success = true, message = "Đã ghi nhận lượt xem CV" });
            return NotFound(new { success = false, message = "Không tìm thấy hồ sơ" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi ghi nhận lượt xem hồ sơ ID: {Id}", id);
            return StatusCode(500, new { success = false, message = "Có lỗi xảy ra" });
        }
    }

    /// <summary>
    /// Lấy điểm AI và breakdown (Explainable AI) của một Application
    /// </summary>
    [HttpGet("{id}/ai-score")]
    [Authorize(Roles = "HR, ADMIN, INTERVIEWER")]
    public async Task<IActionResult> GetAiScore(Guid id)
    {
        try
        {
            var score = await _context.ApplicationAiScores
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.ApplicationId == id);

            if (score == null)
                return NotFound(new { success = false, message = "Chưa có dữ liệu AI scoring cho hồ sơ này." });

            // Deserialize BreakdownJson
            object? breakdown = null;
            if (!string.IsNullOrEmpty(score.BreakdownJson))
            {
                try { breakdown = System.Text.Json.JsonSerializer.Deserialize<object>(score.BreakdownJson); }
                catch { /* ignore malformed json */ }
            }

            // Deserialize MatchedSkillsJson
            object? skillsInfo = null;
            if (!string.IsNullOrEmpty(score.MatchedSkillsJson))
            {
                try { skillsInfo = System.Text.Json.JsonSerializer.Deserialize<object>(score.MatchedSkillsJson); }
                catch { /* ignore */ }
            }

            return Ok(new
            {
                success = true,
                data = new
                {
                    applicationId  = id,
                    matchingScore  = score.MatchingScore,
                    breakdown,
                    skillsInfo,
                    model          = score.Model,
                    createdAt      = score.CreatedAt
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy AI score cho ApplicationId: {Id}", id);
            return StatusCode(500, new { success = false, message = "Có lỗi xảy ra." });
        }
    }

    #region GOOGLE SHEETS & FORM INTEGRATION

    /// <summary>
    /// Phương án 1: Import danh sách ứng viên từ Google Sheets vào chiến dịch tuyển dụng (Dành cho HR/Admin)
    /// </summary>
    /// <param name="request">Chứa JobId và danh sách ứng viên trích xuất từ Google Sheets</param>
    /// <returns>Báo cáo kết quả import (Thành công, Trùng lặp, Bỏ qua)</returns>
    [HttpPost("import-google-sheets")]
    [Authorize]
    public async Task<IActionResult> ImportGoogleSheets([FromBody] ImportGoogleSheetsRequest request)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();

            return BadRequest(new
            {
                success = false,
                message = "Dữ liệu gửi lên không hợp lệ.",
                errors
            });
        }

        try
        {
            var result = await _applicationService.ImportGoogleSheetsAsync(request);

            return Ok(new
            {
                success = true,
                message = $"Xử lý xong {result.TotalReceived} bản ghi: Đã nhập {result.ImportedCount}, Trùng lặp {result.DuplicateCount}, Bỏ qua {result.SkippedCount}.",
                data = result
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi import Google Sheets cho JobId {JobId}", request.JobId);
            return StatusCode(500, new
            {
                success = false,
                message = "Đã xảy ra lỗi trong quá trình lưu trữ hồ sơ ứng viên.",
                detail = ex.Message
            });
        }
    }

    /// <summary>
    /// Đồng bộ 1-click trực tiếp từ đường link Google Sheet công khai (Thao tác hoàn toàn trên web ATS)
    /// </summary>
    [HttpPost("sync-google-sheets")]
    [Authorize]
    public async Task<IActionResult> SyncGoogleSheets([FromBody] SyncGoogleSheetRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { success = false, message = "Vui lòng nhập đầy đủ JobId và đường link Google Sheet." });
        }

        try
        {
            var result = await _applicationService.SyncFromGoogleSheetUrlAsync(request.JobId, request.SheetUrl, request.ColumnMapping);
            return Ok(new
            {
                success = true,
                message = $"Đã đồng bộ xong: Nhập mới {result.ImportedCount}, Trùng lặp {result.DuplicateCount}, Bỏ qua {result.SkippedCount}.",
                data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Lỗi đồng bộ Google Sheet cho Job {JobId}", request.JobId);
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Gửi thử nghiệm một hồ sơ ứng viên mẫu qua Webhook để kiểm tra kết nối ngay trên web
    /// </summary>
    [HttpPost("preview-google-sheets")]
    [Authorize]
    public async Task<IActionResult> PreviewGoogleSheets([FromBody] PreviewGoogleSheetRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.SheetUrl))
            return BadRequest(new { success = false, message = "Vui lòng nhập đường link Google Sheet." });

        try
        {
            var preview = await _applicationService.PreviewGoogleSheetUrlAsync(request.SheetUrl);
            return Ok(new { success = true, data = preview });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [HttpPost("test-webhook")]
    [Authorize]
    public async Task<IActionResult> TestWebhook([FromBody] GoogleFormWebhookRequest request)
    {
        try
        {
            var result = await _applicationService.ProcessGoogleFormWebhookAsync(request);
            return Ok(new
            {
                success = result.Success,
                message = result.Message,
                data = result
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Phương án 2: Webhook tiếp nhận dữ liệu thời gian thực từ Google Forms / Apps Script
    /// </summary>
    /// <param name="webhookSecret">Secret Token truyền trong Header 'X-Webhook-Secret'</param>
    /// <param name="request">Payload ứng viên gửi từ form đăng ký quét mã QR</param>
    [HttpPost("webhook-google-form")]
    [AllowAnonymous]
    public async Task<IActionResult> WebhookGoogleForm(
        [FromHeader(Name = "X-Webhook-Secret")] string? webhookSecret,
        [FromBody] GoogleFormWebhookRequest request)
    {
        // 1. Xác thực bảo mật đơn giản qua Header
        var (_, _, runtimeSecret) = _campaignQrService.GetPublicConfig();
        var configuredSecret = _configuration["GoogleIntegration:WebhookSecret"] ?? "ats_google_webhook_secret_2026";
        var isAuthorized = !string.IsNullOrWhiteSpace(webhookSecret) &&
            (string.Equals(webhookSecret, runtimeSecret, StringComparison.Ordinal) ||
             string.Equals(webhookSecret, configuredSecret, StringComparison.Ordinal));

        if (!isAuthorized)
        {
            _logger.LogWarning("⚠️ [WEBHOOK] Từ chối request do sai Header X-Webhook-Secret. Nhận: {Secret}", webhookSecret);
            return Unauthorized(new
            {
                success = false,
                message = "Xác thực không hợp lệ. Vui lòng cung cấp chính xác Header 'X-Webhook-Secret'."
            });
        }

        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();

            return BadRequest(new
            {
                success = false,
                message = "Dữ liệu ứng tuyển không hợp lệ.",
                errors
            });
        }

        try
        {
            var result = await _applicationService.ProcessGoogleFormWebhookAsync(request);

            if (!result.Success && result.IsDuplicate)
            {
                return Conflict(new
                {
                    success = false,
                    message = result.Message,
                    data = result
                });
            }

            if (!result.Success)
            {
                return BadRequest(new
                {
                    success = false,
                    message = result.Message,
                    data = result
                });
            }

            return Ok(new
            {
                success = true,
                message = "Tiếp nhận ứng viên qua Google Forms thành công.",
                data = result
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi xử lý webhook Google Form cho JobId {JobId}", request.JobId);
            return StatusCode(500, new
            {
                success = false,
                message = "Đã xảy ra lỗi nội bộ khi xử lý webhook.",
                detail = ex.Message
            });
        }
    }

    #endregion
}
