using Microsoft.EntityFrameworkCore;
using UTC_DATN.Data;
using UTC_DATN.Entities;

namespace UTC_DATN.Services.Implements;

// ═══════════════════════════════════════════════════════════════════════════════
// NHIỆM VỤ 3: Data Isolation - Quản lý Hồ sơ CV (Application)
//
// Kiến trúc phân quyền theo "Vòng đời Hồ sơ" (Application Lifecycle):
//
//  [Ứng viên nộp] → [HR sàng lọc] → [Chuyên môn phỏng vấn] → [Quyết định]
//       ↑                 ↑                     ↑
//   Trạng thái:     HR_PENDING           DEPT_PENDING
//                   AI_SCORED            INTERVIEW_SCHEDULED
//
// Mỗi vòng đời, chỉ bộ phận có trách nhiệm mới được xem/xử lý CV.
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Định nghĩa các trạng thái CV theo vòng đời tuyển dụng.
/// Đồng bộ với bảng PipelineStages.Code và Applications.Status.
/// </summary>
public static class ApplicationStatus
{
    // ── Giai đoạn 1: HR xử lý ──────────────────────────────────────────────
    /// <summary>CV vừa nộp, chưa có ai xử lý</summary>
    public const string Pending = "PENDING";

    /// <summary>AI đã chấm điểm, HR đang chờ review</summary>
    public const string AiScored = "AI_SCORED";

    // ── Giai đoạn 2: Chuyên môn phỏng vấn ──────────────────────────────────
    /// <summary>HR đã duyệt, chuyển sang bộ phận chuyên môn phỏng vấn</summary>
    public const string HrApproved = "HR_APPROVED";

    /// <summary>Đã lên lịch phỏng vấn với bộ phận chuyên môn</summary>
    public const string InterviewScheduled = "INTERVIEW_SCHEDULED";

    // ── Giai đoạn 3: Kết quả ────────────────────────────────────────────────
    /// <summary>Đã phỏng vấn xong, chờ kết quả cuối</summary>
    public const string Interviewed = "INTERVIEWED";

    /// <summary>Đề xuất nhận vào làm</summary>
    public const string Offered = "OFFERED";

    /// <summary>Từ chối hồ sơ</summary>
    public const string Rejected = "REJECTED";

    /// <summary>Trúng tuyển</summary>
    public const string Hired = "HIRED";
}

/// <summary>
/// DTO trả về cho danh sách CV / Application.
/// Thiết kế phẳng để hiển thị trực tiếp trên bảng UI mà không cần join phụ.
/// </summary>
public class ApplicationListDto
{
    public Guid ApplicationId { get; set; }
    public DateTime AppliedAt { get; set; }
    public string Status { get; set; }
    public string Source { get; set; }

    // Thông tin ứng viên (Snapshot)
    public string ContactName { get; set; }
    public string ContactEmail { get; set; }
    public string ContactPhone { get; set; }

    // Thông tin Candidate profile
    public Guid CandidateId { get; set; }
    public string CandidateFullName { get; set; }
    public string CandidateHeadline { get; set; }

    // Thông tin Job Posting
    public Guid JobId { get; set; }
    public string JobTitle { get; set; }
    public Guid? JobDepartmentId { get; set; }
    public string JobDepartmentCode { get; set; }
    public string JobDepartmentName { get; set; }

    // Pipeline Stage hiện tại
    public string CurrentStageName { get; set; }
    public string CurrentStageCode { get; set; }

    // AI Score (nếu có)
    public decimal? LatestAiScore { get; set; }
}

public class ApplicationDataIsolationService
{
    private readonly UTC_DATNContext _context;

    public ApplicationDataIsolationService(UTC_DATNContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Lấy danh sách CV ứng viên có áp dụng Data Isolation theo Role.
    ///
    /// RULE PHÂN QUYỀN CHI TIẾT:
    /// ┌─────────────────────────────┬──────────────────────────────────────────────────────────────────────────────────────────────┐
    /// │ Role                        │ Quy tắc truy cập                                                                             │
    /// ├─────────────────────────────┼──────────────────────────────────────────────────────────────────────────────────────────────┤
    /// │ SuperAdmin (ADMIN)          │ Toàn bộ CV — không giới hạn                                                                  │
    /// ├─────────────────────────────┼──────────────────────────────────────────────────────────────────────────────────────────────┤
    /// │ HR_TEAM                     │ CV có Status = PENDING hoặc AI_SCORED (giai đoạn sàng lọc HR)                                │
    /// │                             │ → HR thực hiện review, approve/reject CV vào vòng chuyên môn                                 │
    /// ├─────────────────────────────┼──────────────────────────────────────────────────────────────────────────────────────────────┤
    /// │ Manager (WH_LOGS/TECH_MNT/  │ CV thuộc Job trong DepartmentId của Manager VÀ                                               │
    /// │  PROD_LINE/QA_QC...)        │ Status = HR_APPROVED hoặc INTERVIEW_SCHEDULED                                                │
    /// │                             │ → Chuyên môn chỉ xem CV "đã qua ải HR" và liên quan đến bộ phận mình                        │
    /// ├─────────────────────────────┼──────────────────────────────────────────────────────────────────────────────────────────────┤
    /// │ INT_TEAM (Interviewer)      │ CV đang ở Status = INTERVIEW_SCHEDULED (Được mời phỏng vấn trực tiếp)                       │
    /// │                             │ → Hội đồng chỉ xem CV họ cần phỏng vấn hôm nay                                              │
    /// └─────────────────────────────┴──────────────────────────────────────────────────────────────────────────────────────────────┘
    /// </summary>
    public async Task<IEnumerable<ApplicationListDto>> GetApplicationsAsync(
        CurrentUserContext currentUser,
        string statusFilter = null)
    {
        // === Base Query với đầy đủ navigation ===
        var query = _context.Applications
            .AsNoTracking()
            .Include(a => a.Candidate)
            .Include(a => a.Job)
                .ThenInclude(j => j.DepartmentNavigation)
            .Include(a => a.CurrentStage)
            .Include(a => a.ApplicationAiScores)
            .AsQueryable();

        // ════════════════════════════════════════════════════════════════
        // RULE 1: SuperAdmin — Xem toàn bộ CV không hạn chế
        // ════════════════════════════════════════════════════════════════
        if (currentUser.CurrentUserRole == RoleCodes.SuperAdmin)
        {
            // Không filter, xem toàn bộ
            // Vẫn có thể filter thêm theo statusFilter param nếu UI cần
        }

        // ════════════════════════════════════════════════════════════════
        // RULE 2: HR_TEAM — Xem CV đang chờ sàng lọc (giai đoạn HR)
        //
        // Lý do: HR chỉ cần xem CV chưa xử lý (PENDING) và CV
        // đã có điểm AI (AI_SCORED) để ra quyết định approve/reject.
        // CV đã được approve (HR_APPROVED trở đi) là "done" với HR.
        // ════════════════════════════════════════════════════════════════
        else if (currentUser.CurrentUserDepartmentCode == "HR_TEAM"
                 || currentUser.CurrentUserRole == RoleCodes.HR)
        {
            query = query.Where(a =>
                a.Status == ApplicationStatus.Pending ||
                a.Status == ApplicationStatus.AiScored);
        }

        // ════════════════════════════════════════════════════════════════
        // RULE 3: INT_TEAM (Hội đồng Phỏng vấn) — Chỉ xem CV lên lịch PV
        //
        // Lý do: Interviewer không cần biết pipeline; họ chỉ cần
        // danh sách ứng viên được mời đến hôm nay/tuần này.
        // ════════════════════════════════════════════════════════════════
        else if (currentUser.CurrentUserDepartmentCode == "INT_TEAM"
                 || currentUser.CurrentUserRole == RoleCodes.Interviewer)
        {
            query = query.Where(a =>
                a.Status == ApplicationStatus.InterviewScheduled);
        }

        // ════════════════════════════════════════════════════════════════
        // RULE 4: Manager của Khối Chuyên môn (WH_LOGS, TECH_MNT, PROD_LINE, QA_QC)
        //
        // Điều kiện kép:
        //   (a) CV phải thuộc Job trong phòng ban của Manager (DepartmentId match)
        //   (b) CV phải đã qua ải HR: Status = HR_APPROVED hoặc INTERVIEW_SCHEDULED
        //
        // Lý do: Trưởng phòng Kho không được xem CV đang ở giai đoạn HR,
        // và cũng không xem được CV của khu vực khác (PROD_LINE, QA_QC...).
        // ════════════════════════════════════════════════════════════════
        else if (currentUser.CurrentUserRole == RoleCodes.Manager)
        {
            if (!currentUser.CurrentUserDepartmentId.HasValue)
                return Enumerable.Empty<ApplicationListDto>(); // Cấu hình sai

            query = query.Where(a =>
                // (a) Job phải thuộc phòng ban của Manager
                a.Job.DepartmentId == currentUser.CurrentUserDepartmentId
                &&
                // (b) CV phải đã được HR duyệt hoặc đang lên lịch PV
                (a.Status == ApplicationStatus.HrApproved ||
                 a.Status == ApplicationStatus.InterviewScheduled));
        }

        // ════════════════════════════════════════════════════════════════
        // FALLBACK: Role không xác định → Không trả về gì (Zero Trust)
        // ════════════════════════════════════════════════════════════════
        else
        {
            return Enumerable.Empty<ApplicationListDto>();
        }

        // === Optional: Lọc thêm theo status từ UI request ===
        if (!string.IsNullOrWhiteSpace(statusFilter))
        {
            query = query.Where(a => a.Status == statusFilter);
        }

        // === Project sang DTO ===
        var result = await query
            .OrderByDescending(a => a.AppliedAt)
            .Select(a => new ApplicationListDto
            {
                ApplicationId     = a.ApplicationId,
                AppliedAt         = a.AppliedAt,
                Status            = a.Status,
                Source            = a.Source,

                // Snapshot contact (không bị thay đổi nếu Candidate cập nhật profile)
                ContactName       = a.ContactName,
                ContactEmail      = a.ContactEmail,
                ContactPhone      = a.ContactPhone,

                // Candidate profile hiện tại
                CandidateId       = a.CandidateId,
                CandidateFullName = a.Candidate != null ? a.Candidate.FullName : a.ContactName,
                CandidateHeadline = a.Candidate != null ? a.Candidate.Headline : null,

                // Job info
                JobId             = a.JobId,
                JobTitle          = a.Job != null ? a.Job.Title : null,
                JobDepartmentId   = a.Job != null ? a.Job.DepartmentId : null,
                JobDepartmentCode = a.Job != null && a.Job.DepartmentNavigation != null
                                        ? a.Job.DepartmentNavigation.DepartmentCode
                                        : null,
                JobDepartmentName = a.Job != null && a.Job.DepartmentNavigation != null
                                        ? a.Job.DepartmentNavigation.DepartmentName
                                        : null,

                // Pipeline stage
                CurrentStageName  = a.CurrentStage != null ? a.CurrentStage.Name : null,
                CurrentStageCode  = a.CurrentStage != null ? a.CurrentStage.Code : null,

                // AI Score mới nhất (nếu có)
                LatestAiScore     = a.ApplicationAiScores
                                     .OrderByDescending(s => s.CreatedAt)
                                     .Select(s => (decimal?)s.MatchingScore)
                                     .FirstOrDefault()
            })
            .ToListAsync();

        return result;
    }

    /// <summary>
    /// Overload phân trang + tìm kiếm cho màn hình quản lý CV (mass recruitment).
    /// </summary>
    public async Task<(IEnumerable<ApplicationListDto> Items, int TotalCount)> GetApplicationsPagedAsync(
        CurrentUserContext currentUser,
        int pageIndex = 1,
        int pageSize = 20,
        string searchKeyword = null,
        string statusFilter = null)
    {
        var query = _context.Applications
            .AsNoTracking()
            .Include(a => a.Candidate)
            .Include(a => a.Job)
                .ThenInclude(j => j.DepartmentNavigation)
            .Include(a => a.CurrentStage)
            .Include(a => a.ApplicationAiScores)
            .AsQueryable();

        // Áp dụng cùng Data Isolation rules
        if (currentUser.CurrentUserRole == RoleCodes.SuperAdmin)
        { /* no filter */ }
        else if (currentUser.CurrentUserDepartmentCode == "HR_TEAM"
                 || currentUser.CurrentUserRole == RoleCodes.HR)
        {
            query = query.Where(a =>
                a.Status == ApplicationStatus.Pending ||
                a.Status == ApplicationStatus.AiScored);
        }
        else if (currentUser.CurrentUserDepartmentCode == "INT_TEAM"
                 || currentUser.CurrentUserRole == RoleCodes.Interviewer)
        {
            query = query.Where(a =>
                a.Status == ApplicationStatus.InterviewScheduled);
        }
        else if (currentUser.CurrentUserRole == RoleCodes.Manager)
        {
            if (!currentUser.CurrentUserDepartmentId.HasValue)
                return (Enumerable.Empty<ApplicationListDto>(), 0);

            query = query.Where(a =>
                a.Job.DepartmentId == currentUser.CurrentUserDepartmentId &&
                (a.Status == ApplicationStatus.HrApproved ||
                 a.Status == ApplicationStatus.InterviewScheduled));
        }
        else
        {
            return (Enumerable.Empty<ApplicationListDto>(), 0);
        }

        // Lọc theo status từ UI
        if (!string.IsNullOrWhiteSpace(statusFilter))
            query = query.Where(a => a.Status == statusFilter);

        // Tìm kiếm theo tên ứng viên hoặc tiêu đề job
        if (!string.IsNullOrWhiteSpace(searchKeyword))
        {
            var keyword = searchKeyword.ToLower().Trim();
            query = query.Where(a =>
                (a.ContactName != null && a.ContactName.ToLower().Contains(keyword)) ||
                (a.Candidate != null && a.Candidate.FullName.ToLower().Contains(keyword)) ||
                (a.Job != null && a.Job.Title.ToLower().Contains(keyword)));
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(a => a.AppliedAt)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new ApplicationListDto
            {
                ApplicationId     = a.ApplicationId,
                AppliedAt         = a.AppliedAt,
                Status            = a.Status,
                Source            = a.Source,
                ContactName       = a.ContactName,
                ContactEmail      = a.ContactEmail,
                ContactPhone      = a.ContactPhone,
                CandidateId       = a.CandidateId,
                CandidateFullName = a.Candidate != null ? a.Candidate.FullName : a.ContactName,
                CandidateHeadline = a.Candidate != null ? a.Candidate.Headline : null,
                JobId             = a.JobId,
                JobTitle          = a.Job != null ? a.Job.Title : null,
                JobDepartmentId   = a.Job != null ? a.Job.DepartmentId : null,
                JobDepartmentCode = a.Job != null && a.Job.DepartmentNavigation != null
                                        ? a.Job.DepartmentNavigation.DepartmentCode : null,
                JobDepartmentName = a.Job != null && a.Job.DepartmentNavigation != null
                                        ? a.Job.DepartmentNavigation.DepartmentName : null,
                CurrentStageName  = a.CurrentStage != null ? a.CurrentStage.Name : null,
                CurrentStageCode  = a.CurrentStage != null ? a.CurrentStage.Code : null,
                LatestAiScore     = a.ApplicationAiScores
                                     .OrderByDescending(s => s.CreatedAt)
                                     .Select(s => (decimal?)s.MatchingScore)
                                     .FirstOrDefault()
            })
            .ToListAsync();

        return (items, totalCount);
    }
}
