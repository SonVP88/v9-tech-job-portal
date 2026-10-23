using Microsoft.EntityFrameworkCore;
using UTC_DATN.Data;
using UTC_DATN.Entities;

namespace UTC_DATN.Services.Implements;

/// <summary>
/// Context (Claims) của người dùng hiện tại được extract từ JWT Token.
/// Inject từ IHttpContextAccessor trong Controller hoặc Service.
/// </summary>
public class CurrentUserContext
{
    /// <summary>UserId của người đang đăng nhập</summary>
    public Guid CurrentUserId { get; set; }

    /// <summary>
    /// Role code của người dùng: "SuperAdmin" | "Manager" | "Staff"
    /// Mapping với bảng Roles.Code trong DB.
    /// </summary>
    public string CurrentUserRole { get; set; }

    /// <summary>
    /// DepartmentCode của người dùng (lấy từ claim "department_code").
    /// Ví dụ: "HR_TEAM", "WH_LOGS", "PROD_LINE"
    /// </summary>
    public string CurrentUserDepartmentCode { get; set; }

    /// <summary>DepartmentId của người dùng (lấy từ claim "department_id")</summary>
    public Guid? CurrentUserDepartmentId { get; set; }
}

// ═══════════════════════════════════════════════════════════════════════════════
// NHIỆM VỤ 2: Data Isolation - Quản lý Nhân sự (User)
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Các hằng số Role Code được dùng xuyên suốt hệ thống.
/// Đồng bộ với bảng Roles.Code trong DB.
/// </summary>
public static class RoleCodes
{
    /// <summary>Ban Giám đốc / Super Admin hệ thống — toàn quyền</summary>
    public const string SuperAdmin = "ADMIN";

    /// <summary>Trưởng phòng / Trưởng khu vực — xem nhân viên trong bộ phận mình</summary>
    public const string Manager = "MANAGER";

    /// <summary>Nhân viên / Công nhân — chỉ xem thông tin của chính mình</summary>
    public const string Staff = "STAFF";

    /// <summary>Nhân viên HR — quản lý CV và pipeline tuyển dụng</summary>
    public const string HR = "HR";

    /// <summary>Hội đồng Phỏng vấn — xem CV đã được HR duyệt cho vòng chuyên môn</summary>
    public const string Interviewer = "INTERVIEWER";
}

/// <summary>
/// DTO trả về cho danh sách User (phẳng hóa, không expose sensitive data).
/// </summary>
public class UserListDto
{
    public Guid UserId { get; set; }
    public string FullName { get; set; }
    public string Email { get; set; }
    public string Phone { get; set; }
    public string AvatarUrl { get; set; }
    public bool IsActive { get; set; }
    public Guid? DepartmentId { get; set; }
    public string DepartmentCode { get; set; }
    public string DepartmentName { get; set; }
    public Guid? ManagerId { get; set; }
    public string ManagerName { get; set; }
    public IEnumerable<string> Roles { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class UserDataIsolationService
{
    private readonly UTC_DATNContext _context;

    public UserDataIsolationService(UTC_DATNContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Lấy danh sách nhân viên có áp dụng Data Isolation theo Role của người dùng hiện tại.
    ///
    /// RULE PHÂN QUYỀN:
    /// ┌─────────────────┬──────────────────────────────────────────────────────────────────┐
    /// │ Role            │ Quy tắc truy cập                                                 │
    /// ├─────────────────┼──────────────────────────────────────────────────────────────────┤
    /// │ SuperAdmin      │ Toàn bộ Users trong hệ thống (không filter)                      │
    /// │ Manager         │ Chỉ Users thuộc DepartmentId của Manager đó                      │
    /// │ Staff / Default │ Chỉ chính bản thân (UserId == CurrentUserId)                     │
    /// └─────────────────┴──────────────────────────────────────────────────────────────────┘
    /// </summary>
    public async Task<IEnumerable<UserListDto>> GetUsersAsync(CurrentUserContext currentUser)
    {
        // === Base Query: Include navigation properties cần thiết ===
        var query = _context.Users
            .AsNoTracking()
            .Where(u => u.IsActive) // Chỉ lấy user đang hoạt động
            .Include(u => u.Department)
            .Include(u => u.Manager)
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .AsQueryable();

        // ════════════════════════════════════════════════════════════════
        // RULE 1: SuperAdmin — Không filter, xem toàn bộ
        // ════════════════════════════════════════════════════════════════
        if (currentUser.CurrentUserRole == RoleCodes.SuperAdmin)
        {
            // Không áp dụng WHERE clause, lấy tất cả
        }

        // ════════════════════════════════════════════════════════════════
        // RULE 2: Manager (Trưởng bộ phận) — Chỉ xem User trong Dept mình
        //
        // Ví dụ: Trưởng phòng Kho (WH_LOGS) chỉ thấy công nhân kho,
        //        không thấy nhân viên PROD_LINE hay TECH_MNT.
        // ════════════════════════════════════════════════════════════════
        else if (currentUser.CurrentUserRole == RoleCodes.Manager)
        {
            if (!currentUser.CurrentUserDepartmentId.HasValue)
                return Enumerable.Empty<UserListDto>(); // Cấu hình sai, trả về rỗng

            query = query.Where(u => u.DepartmentId == currentUser.CurrentUserDepartmentId);
        }

        // ════════════════════════════════════════════════════════════════
        // RULE 3: Staff / Nhân viên — Chỉ xem thông tin của chính mình
        // ════════════════════════════════════════════════════════════════
        else
        {
            query = query.Where(u => u.UserId == currentUser.CurrentUserId);
        }

        // === Project sang DTO (tránh expose sensitive fields như PasswordHash) ===
        var result = await query
            .OrderBy(u => u.Department != null ? u.Department.DepartmentCode : "ZZZ")
            .ThenBy(u => u.FullName)
            .Select(u => new UserListDto
            {
                UserId         = u.UserId,
                FullName       = u.FullName,
                Email          = u.Email,
                Phone          = u.Phone,
                AvatarUrl      = u.AvatarUrl,
                IsActive       = u.IsActive,
                DepartmentId   = u.DepartmentId,
                DepartmentCode = u.Department != null ? u.Department.DepartmentCode : null,
                DepartmentName = u.Department != null ? u.Department.DepartmentName : null,
                ManagerId      = u.ManagerId,
                ManagerName    = u.Manager != null ? u.Manager.FullName : null,
                Roles          = u.UserRoles.Select(ur => ur.Role.Code),
                CreatedAt      = u.CreatedAt
            })
            .ToListAsync();

        return result;
    }

    /// <summary>
    /// Overload hỗ trợ phân trang (pagination) — dùng cho màn hình danh sách lớn.
    /// </summary>
    public async Task<(IEnumerable<UserListDto> Items, int TotalCount)> GetUsersPagedAsync(
        CurrentUserContext currentUser,
        int pageIndex = 1,
        int pageSize = 20,
        string searchKeyword = null)
    {
        var query = _context.Users
            .AsNoTracking()
            .Where(u => u.IsActive)
            .Include(u => u.Department)
            .Include(u => u.Manager)
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .AsQueryable();

        // Áp dụng cùng rule Data Isolation
        if (currentUser.CurrentUserRole == RoleCodes.SuperAdmin)
        { /* no filter */ }
        else if (currentUser.CurrentUserRole == RoleCodes.Manager)
        {
            if (!currentUser.CurrentUserDepartmentId.HasValue)
                return (Enumerable.Empty<UserListDto>(), 0);
            query = query.Where(u => u.DepartmentId == currentUser.CurrentUserDepartmentId);
        }
        else
        {
            query = query.Where(u => u.UserId == currentUser.CurrentUserId);
        }

        // Tìm kiếm theo tên hoặc email
        if (!string.IsNullOrWhiteSpace(searchKeyword))
        {
            var keyword = searchKeyword.ToLower().Trim();
            query = query.Where(u =>
                u.FullName.ToLower().Contains(keyword) ||
                u.Email.ToLower().Contains(keyword));
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(u => u.FullName)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new UserListDto
            {
                UserId         = u.UserId,
                FullName       = u.FullName,
                Email          = u.Email,
                Phone          = u.Phone,
                AvatarUrl      = u.AvatarUrl,
                IsActive       = u.IsActive,
                DepartmentId   = u.DepartmentId,
                DepartmentCode = u.Department != null ? u.Department.DepartmentCode : null,
                DepartmentName = u.Department != null ? u.Department.DepartmentName : null,
                ManagerId      = u.ManagerId,
                ManagerName    = u.Manager != null ? u.Manager.FullName : null,
                Roles          = u.UserRoles.Select(ur => ur.Role.Code),
                CreatedAt      = u.CreatedAt
            })
            .ToListAsync();

        return (items, totalCount);
    }
}
