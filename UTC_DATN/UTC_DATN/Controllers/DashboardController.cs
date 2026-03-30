using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using UTC_DATN.DTOs;
using UTC_DATN.Data;

namespace UTC_DATN.Controllers;

[ApiController]
[Route("api/dashboard")]
// [Authorize(Roles = "HR,ADMIN")] // Tạm thời disable để test
public class DashboardController : ControllerBase
{
    private readonly UTC_DATNContext _context;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(UTC_DATNContext context, ILogger<DashboardController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// GET /api/dashboard/summary - 4 summary cards
    /// </summary>
    [HttpGet("summary")]
    [ResponseCache(Duration = 60, Location = ResponseCacheLocation.Any)] // Cache 1 phút
    public async Task<ActionResult<DashboardSummaryDto>> GetSummary()
    {
        try
        {
            var now = DateTime.Now;
            var sevenDaysAgo = now.AddDays(-7);
            var lastMonth = now.AddMonths(-1);

            // 1. Total Candidates (unique trong toàn bộ hệ thống)
            var totalCandidates = await _context.Applications
                .AsNoTracking()
                .Select(a => a.CandidateId)
                .Distinct()
                .CountAsync();

            var totalCandidatesLastMonth = await _context.Applications
                .AsNoTracking()
                .Where(a => a.AppliedAt <= lastMonth)
                .Select(a => a.CandidateId)
                .Distinct()
                .CountAsync();

            // 2. Open Positions
            var openPositions = await _context.Jobs
                .AsNoTracking()
                .Where(j => !j.IsDeleted && (j.Status == "ACTIVE" || j.Status == "OPEN"))
                .CountAsync();

            var openPositionsLastMonth = await _context.Jobs
                .AsNoTracking()
                .Where(j => !j.IsDeleted && (j.Status == "ACTIVE" || j.Status == "OPEN") && j.CreatedAt <= lastMonth)
                .CountAsync();

            // 3. Interviews Today
            var today = now.Date;
            var tomorrow = today.AddDays(1);
            
            var interviewsToday = await _context.Interviews
                .AsNoTracking()
                .Where(i => i.ScheduledStart >= today && i.ScheduledStart < tomorrow)
                .CountAsync();

            var yesterday = today.AddDays(-1);
            var interviewsYesterday = await _context.Interviews
                .AsNoTracking()
                .Where(i => i.ScheduledStart >= yesterday && i.ScheduledStart < today)
                .CountAsync();

            // 4. New Applications (7 ngày gần đây)
            var newApplications = await _context.Applications
                .AsNoTracking()
                .Where(a => a.AppliedAt >= sevenDaysAgo)
                .CountAsync();

            var previousPeriodStart = sevenDaysAgo.AddDays(-7);
            var newApplicationsPrevious = await _context.Applications
                .AsNoTracking()
                .Where(a => a.AppliedAt >= previousPeriodStart && a.AppliedAt < sevenDaysAgo)
                .CountAsync();

            // Tính growth rate
            var summary = new DashboardSummaryDto
            {
                TotalCandidates = totalCandidates,
                OpenPositions = openPositions,
                InterviewsToday = interviewsToday,
                NewApplications = newApplications,
                TotalCandidatesGrowth = CalculateGrowth(totalCandidates, totalCandidatesLastMonth),
                OpenPositionsGrowth = CalculateGrowth(openPositions, openPositionsLastMonth),
                InterviewsTodayGrowth = CalculateGrowth(interviewsToday, interviewsYesterday),
                NewApplicationsGrowth = CalculateGrowth(newApplications, newApplicationsPrevious)
            };

            return Ok(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi tải dashboard summary");
            return StatusCode(500, new { message = "Lỗi server khi tải dữ liệu dashboard" });
        }
    }

    /// <summary>
    /// GET /api/dashboard/activity - Recent activity log
    /// </summary>
    [HttpGet("activity")]
    [ResponseCache(Duration = 30, Location = ResponseCacheLocation.Any)] // Cache 30 giây
    public async Task<ActionResult<List<DashboardActivityDto>>> GetRecentActivity([FromQuery] int count = 10)
    {
        try
        {
            var activities = new List<DashboardActivityDto>();

            // Lấy applications gần nhất
            var recentApplications = await _context.Applications
                .AsNoTracking()
                .Include(a => a.Candidate)
                .Include(a => a.Job)
                .OrderByDescending(a => a.AppliedAt)
                .Take(count / 2)
                .Select(a => new DashboardActivityDto
                {
                    Type = "new_application",
                    Title = $"Ứng tuyển mới: {a.Job!.Title}",
                    Description = $"{a.Candidate!.FullName} đã nộp hồ sơ qua {a.Source ?? "Website"}.",
                    Timestamp = a.AppliedAt,
                    Icon = "person_add",
                    IconColor = "blue"
                })
                .ToListAsync();

            activities.AddRange(recentApplications);

            // Lấy interviews được confirm gần nhất
            var recentInterviews = await _context.Interviews
                .AsNoTracking()
                .Include(i => i.Application)
                .ThenInclude(a => a.Candidate)
                .Where(i => i.Status == "SCHEDULED")
                .OrderByDescending(i => i.CreatedAt)
                .Take(count / 4)
                .Select(i => new DashboardActivityDto
                {
                    Type = "interview_confirmed",
                    Title = "Phỏng vấn đã xác nhận",
                    Description = $"{i.Application.Candidate!.FullName} đã chấp nhận lời mời phỏng vấn.",
                    Timestamp = i.CreatedAt,
                    Icon = "check_circle",
                    IconColor = "green"
                })
                .ToListAsync();

            activities.AddRange(recentInterviews);

            // Lấy offers gần nhất
            var recentOffers = await _context.Applications
                .AsNoTracking()
                .Include(a => a.Candidate)
                .Include(a => a.Job)
                .Where(a => a.Status == "Offer_Sent")
                .OrderByDescending(a => a.LastStageChangedAt)
                .Take(count / 4)
                .Select(a => new DashboardActivityDto
                {
                    Type = "offer_sent",
                    Title = "Đã gửi thư Mời nhận việc",
                    Description = $"Đã gửi cho {a.Candidate!.FullName} cho vị trí {a.Job!.Title}.",
                    Timestamp = a.LastStageChangedAt,
                    Icon = "assignment",
                    IconColor = "orange"
                })
                .ToListAsync();

            activities.AddRange(recentOffers);

            // Sort by timestamp và giới hạn số lượng
            var sortedActivities = activities
                .OrderByDescending(a => a.Timestamp)
                .Take(count)
                .ToList();

            return Ok(sortedActivities);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi tải recent activity");
            return StatusCode(500, new { message = "Lỗi server khi tải hoạt động gần đây" });
        }
    }

    /// <summary>
    /// GET /api/dashboard/activities - Paged recent activity list
    /// </summary>
    [HttpGet("activities")]
    [ResponseCache(Duration = 10, Location = ResponseCacheLocation.Any)] // Cache 10s
    public async Task<ActionResult<PagedActivityDto>> GetPagedActivities([FromQuery] int page = 1, [FromQuery] int pageSize = 15)
    {
        try
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 15;

            var maxFetch = 500; // Giới hạn lấy 500 records mỗi loại cho nhẹ
            var activities = new List<DashboardActivityDto>();

            var recentApplications = await _context.Applications
                .AsNoTracking()
                .Include(a => a.Candidate)
                .Include(a => a.Job)
                .OrderByDescending(a => a.AppliedAt)
                .Take(maxFetch)
                .Select(a => new DashboardActivityDto
                {
                    Type = "new_application",
                    Title = $"Ứng tuyển mới: {a.Job!.Title}",
                    Description = $"{a.Candidate!.FullName} đã nộp hồ sơ qua {a.Source ?? "Website"}.",
                    Timestamp = a.AppliedAt,
                    Icon = "person_add",
                    IconColor = "blue"
                })
                .ToListAsync();

            activities.AddRange(recentApplications);

            var recentInterviews = await _context.Interviews
                .AsNoTracking()
                .Include(i => i.Application)
                .ThenInclude(a => a.Candidate)
                .Where(i => i.Status == "SCHEDULED")
                .OrderByDescending(i => i.CreatedAt)
                .Take(maxFetch)
                .Select(i => new DashboardActivityDto
                {
                    Type = "interview_confirmed",
                    Title = "Phỏng vấn đã xác nhận",
                    Description = $"{i.Application.Candidate!.FullName} đã chấp nhận lời mời phỏng vấn.",
                    Timestamp = i.CreatedAt,
                    Icon = "check_circle",
                    IconColor = "green"
                })
                .ToListAsync();

            activities.AddRange(recentInterviews);

            var recentOffers = await _context.Applications
                .AsNoTracking()
                .Include(a => a.Candidate)
                .Include(a => a.Job)
                .Where(a => a.Status == "Offer_Sent" || a.Status == "HIRED")
                .OrderByDescending(a => a.LastStageChangedAt)
                .Take(maxFetch)
                .Select(a => new DashboardActivityDto
                {
                    Type = a.Status == "HIRED" ? "hired" : "offer_sent",
                    Title = a.Status == "HIRED" ? "Đã tuyển dụng" : "Đã gửi thư Mời nhận việc",
                    Description = a.Status == "HIRED" ? $"Chúc mừng! Đã tuyển {a.Candidate!.FullName} cho vị trí {a.Job!.Title}." : $"Đã gửi cho {a.Candidate!.FullName} cho vị trí {a.Job!.Title}.",
                    Timestamp = a.LastStageChangedAt,
                    Icon = a.Status == "HIRED" ? "workspace_premium" : "assignment",
                    IconColor = a.Status == "HIRED" ? "purple" : "orange"
                })
                .ToListAsync();

            activities.AddRange(recentOffers);

            // In-memory sort and pagination
            var sortedActivities = activities.OrderByDescending(x => x.Timestamp).ToList();
            var totalItems = sortedActivities.Count;
            var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);
            var pagedItems = sortedActivities.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            var result = new PagedActivityDto
            {
                TotalItems = totalItems,
                TotalPages = totalPages,
                CurrentPage = page,
                Items = pagedItems
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi tải paged activity");
            return StatusCode(500, new { message = "Lỗi server khi tải trang hoạt động" });
        }
    }


    /// <summary>
    /// GET /api/dashboard/candidates - Latest candidates cho table
    /// </summary>
    [HttpGet("candidates")]
    [ResponseCache(Duration = 60, Location = ResponseCacheLocation.Any)] // Cache 1 phút
    public async Task<ActionResult<List<DashboardCandidateDto>>> GetLatestCandidates([FromQuery] int count = 10)
    {
        try
        {
            var candidates = await _context.Applications
                .AsNoTracking()
                .Include(a => a.Candidate)
                .Include(a => a.Job)
                .OrderByDescending(a => a.AppliedAt)
                .Take(count)
                .Select(a => new DashboardCandidateDto
                {
                    ApplicationId = a.ApplicationId,
                    JobId = a.JobId,
                    CandidateName = a.Candidate!.FullName,
                    JobTitle = a.Job!.Title,
                    Status = a.Status,
                    StatusLabel = GetStatusLabel(a.Status),
                    StatusColor = GetStatusColor(a.Status),
                    AppliedAt = a.AppliedAt,
                    AvatarUrl = null // Có thể thêm sau
                })
                .ToListAsync();

            return Ok(candidates);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi tải latest candidates");
            return StatusCode(500, new { message = "Lỗi server khi tải danh sách ứng viên" });
        }
    }

    /// <summary>
    /// GET /api/dashboard/weekly-activity - Data cho weekly chart (5 tuần gần nhất)
    /// </summary>
    [HttpGet("weekly-activity")]
    [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Any)] // Cache 5 phút
    public async Task<ActionResult<WeeklyActivityDto>> GetWeeklyActivity([FromQuery] int weeks = 5)
    {
        try
        {
            var result = new WeeklyActivityDto();
            var now = DateTime.Now;

            for (int i = weeks - 1; i >= 0; i--)
            {
                var weekEnd = now.AddDays(-i * 7);
                var weekStart = weekEnd.AddDays(-7);

                result.Labels.Add($"Wk {weeks - i}");

                // Đếm applications trong tuần
                var applicationsCount = await _context.Applications
                    .AsNoTracking()
                    .Where(a => a.AppliedAt >= weekStart && a.AppliedAt < weekEnd)
                    .CountAsync();

                result.ApplicationsData.Add(applicationsCount);

                // Đếm hires trong tuần
                var hiresCount = await _context.Applications
                    .AsNoTracking()
                    .Where(a => a.Status == "HIRED" && 
                               a.LastStageChangedAt >= weekStart && 
                               a.LastStageChangedAt < weekEnd)
                    .CountAsync();

                result.HiresData.Add(hiresCount);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi tải weekly activity");
            return StatusCode(500, new { message = "Lỗi server khi tải dữ liệu biểu đồ" });
        }
    }

    // Helper methods
    private static double CalculateGrowth(int current, int previous)
    {
        if (previous == 0) return current > 0 ? 100 : 0;
        return Math.Round(((double)(current - previous) / previous) * 100, 1);
    }

    private static string GetStatusLabel(string status)
    {
        return status switch
        {
            "PENDING" => "Đã Ứng tuyển",
            "INTERVIEW" => "Phỏng vấn",
            "Pending_Offer" => "Chờ Offer",
            "Offer_Sent" => "Offer đã gửi",
            "OFFER_ACCEPTED" => "Đã đồng ý Offer",
            "HIRED" => "Đã tuyển",
            "REJECTED" => "Từ chối",
            _ => status
        };
    }

    private static string GetStatusColor(string status)
    {
        return status switch
        {
            "PENDING" => "green",
            "INTERVIEW" => "blue",
            "Pending_Offer" => "yellow",
            "Offer_Sent" => "orange",
            "OFFER_ACCEPTED" => "indigo",
            "HIRED" => "green",
            "REJECTED" => "red",
            _ => "gray"
        };
    }

    /// <summary>
    /// GET /api/dashboard/sla-alerts - Lấy SLA alerts/notifications không đọc cho HR
    /// </summary>
    [HttpGet("sla-alerts")]
    [Authorize(Roles = "HR,ADMIN")]
    public async Task<ActionResult<List<DashboardActivityDto>>> GetSlaAlerts()
    {
        try
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userIdGuid))
            {
                return Unauthorized();
            }

            // Lấy unread notifications của user, filter chỉ SLA types
            var slaNotifications = await _context.Notifications
                .AsNoTracking()
                .Where(n => n.UserId == userIdGuid && !n.IsRead)
                .Where(n => n.Type.StartsWith("SLA_"))
                .OrderByDescending(n => n.CreatedAt)
                .Take(20)
                .Select(n => new DashboardActivityDto
                {
                    Type = n.Type,
                    Title = n.Title,
                    Description = n.Message,
                    Timestamp = n.CreatedAt,
                    Icon = GetSlaAlertIcon(n.Type),
                    IconColor = GetSlaAlertColor(n.Type)
                })
                .ToListAsync();

            return Ok(slaNotifications);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Lỗi lấy SLA alerts: {ex.Message}");
            return StatusCode(500, new { message = "Lỗi lấy dữ liệu SLA alerts" });
        }
    }

    /// <summary>
    /// Helper: xác định icon cho SLA alert
    /// </summary>
    private string GetSlaAlertIcon(string notificationType)
    {
        return notificationType switch
        {
            "SLA_WARNING" => "warning",
            "SLA_OVERDUE" => "error",
            "SLA_SEVERE_OVERDUE" => "crisis_alert",
            _ => "notifications"
        };
    }

    /// <summary>
    /// Helper: xác định màu cho SLA alert
    /// </summary>
    private string GetSlaAlertColor(string notificationType)
    {
        return notificationType switch
        {
            "SLA_WARNING" => "orange",
            "SLA_OVERDUE" => "red",
            "SLA_SEVERE_OVERDUE" => "red",
            _ => "gray"
        };
    }
}
