using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using UTC_DATN.Data;
using UTC_DATN.DTOs.Employee;
using UTC_DATN.Services.Interfaces;

namespace UTC_DATN.Controllers
{
    [ApiController]
    [Route("api/employees")]
    public class EmployeeController : ControllerBase
    {
        private readonly IEmployeeService _employeeService;
        private readonly UTC_DATNContext _context;

        public EmployeeController(IEmployeeService employeeService, UTC_DATNContext context)
        {
            _employeeService = employeeService;
            _context = context;
        }

        /// <summary>
        /// Lấy danh sách nhân viên (HR, Manager, Admin)
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "ADMIN,HR,MANAGER")]
        public async Task<IActionResult> GetEmployees()
        {
            var employees = await _employeeService.GetEmployeesAsync();
            return Ok(employees);
        }

        /// <summary>
        /// Lấy danh mục phòng ban và số lượng nhân sự
        /// </summary>
        [HttpGet("departments")]
        [Authorize(Roles = "ADMIN,HR,MANAGER")]
        public async Task<IActionResult> GetDepartments()
        {
            var depts = await _employeeService.GetDepartmentsAsync();
            return Ok(depts);
        }

        /// <summary>
        /// Lấy danh sách quản lý / trưởng phòng cho dropdown gán người quản lý
        /// </summary>
        [HttpGet("managers")]
        [Authorize(Roles = "ADMIN,HR,MANAGER")]
        public async Task<IActionResult> GetManagers()
        {
            var managers = await _employeeService.GetManagersAsync();
            return Ok(managers);
        }

        /// <summary>
    /// Lấy danh sách Interviewer (dùng cho dropdown lên lịch phỏng vấn)
    /// Bao gồm: ADMIN, HR_MANAGER, INTERVIEWER
    /// Cho phép HR và ADMIN truy cập
    /// </summary>
    [HttpGet("interviewers")]
    [Authorize(Roles = "HR, ADMIN")]
    public async Task<IActionResult> GetInterviewers()
    {
        var employees = await _employeeService.GetEmployeesAsync();
        
        // Filter lấy INTERVIEWER, HR, hoặc ADMIN
        var validRoles = new[] { "INTERVIEWER", "HR", "ADMIN" };
        
        var interviewers = employees
            .Where(e => validRoles.Contains(e.Role) && e.IsActive)
            .Select(e => new
            {
                id = e.UserId,
                fullName = e.FullName,
                email = e.Email,
                roleName = e.Role,
                // Helper field for sorting
                rolePriority = e.Role switch
                {
                    "ADMIN" => 1,
                    "HR" => 2,
                    "INTERVIEWER" => 3,
                    _ => 999
                }
            })
            .OrderBy(e => e.rolePriority)  // Ưu tiên theo role: Admin -> HR -> Interviewer
            .ThenBy(e => e.fullName)        // Sau đó sắp xếp theo tên
            .Select(e => new
            {
                e.id,
                e.fullName,
                e.email,
                e.roleName
            })
            .ToList();

        return Ok(interviewers);
    }

        /// <summary>
        /// Tạo nhân viên mới tuân thủ ủy quyền phân cấp (Delegated Administration)
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "ADMIN,HR,MANAGER")]
        public async Task<IActionResult> CreateEmployee([FromBody] CreateEmployeeRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var currentRole = User.FindFirst(ClaimTypes.Role)?.Value ?? "";
            if (!Guid.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out Guid currentUserId))
            {
                return Unauthorized();
            }

            // ── KIỂM TRA ỦY QUYỀN PHÂN CẤP (DELEGATED ADMINISTRATION) ──
            if (currentRole == "MANAGER")
            {
                var currentDbUser = await _context.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.UserId == currentUserId);
                var currentDeptId = currentDbUser?.DepartmentId;

                // Check 1: DepartmentId bắt buộc phải trùng với phòng ban của Manager
                if (request.DepartmentId.HasValue && request.DepartmentId != currentDeptId)
                {
                    return StatusCode(403, new { message = "Trưởng phòng chỉ được phép tạo tài khoản trong phòng ban/xưởng của mình." });
                }

                // Tự động gán DepartmentId của Manager nếu chưa có
                request.DepartmentId = currentDeptId;

                // Check 2: Role được gán BẮT BUỘC phải cấp thấp hơn Manager (chỉ được tạo HR, INTERVIEWER, TeamLeader, Staff)
                if (request.Role == "ADMIN" || request.Role == "MANAGER")
                {
                    return StatusCode(403, new { message = "Trưởng phòng chỉ được cấp tài khoản cấp dưới, không được phép tạo Admin hoặc Manager khác." });
                }

                // Gán người quản lý trực tiếp mặc định là chính Manager này
                if (!request.ManagerId.HasValue)
                {
                    request.ManagerId = currentUserId;
                }
            }
            else if (currentRole != "ADMIN")
            {
                // HR / TeamLeader / Staff không được phép tạo tài khoản
                return StatusCode(403, new { message = "Bạn không có quyền tạo tài khoản nhân sự nội bộ." });
            }

            var employee = await _employeeService.CreateEmployeeAsync(request);

            if (employee == null)
            {
                return BadRequest(new { message = "Email đã tồn tại hoặc dữ liệu không hợp lệ." });
            }

            return CreatedAtAction(nameof(GetEmployees), new { id = employee.UserId }, employee);
        }

        /// <summary>
        /// PATCH /api/users/{id}/toggle-status hoặc /api/employees/{id}/toggle-status
        /// Khóa / Mở khóa tài khoản nhân sự (Xóa mềm thay cho DELETE)
        /// </summary>
        [HttpPatch("{id}/toggle-status")]
        [HttpPatch("/api/users/{id}/toggle-status")]
        [Authorize]
        public async Task<IActionResult> ToggleStatus(Guid id, [FromBody] DeactivateRequest? request = null)
        {
            var currentRole = User.FindFirst(ClaimTypes.Role)?.Value ?? "";
            if (currentRole != "ADMIN" && currentRole != "MANAGER")
            {
                return StatusCode(403, new { message = "Bạn không có quyền thực hiện thao tác này." });
            }

            if (!Guid.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out Guid currentUserId))
            {
                return Unauthorized();
            }

            var fullName = User.FindFirst("FullName")?.Value ?? "User";
            var adminName = $"{fullName} ({currentRole})";

            // Target user - Cần IgnoreQueryFilters để tìm thấy cả user đang bị khóa
            var targetUser = await _context.Users
                .IgnoreQueryFilters()
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.UserId == id);

            if (targetUser == null)
            {
                return NotFound(new { message = "Không tìm thấy người dùng." });
            }

            if (targetUser.UserId == currentUserId)
            {
                return BadRequest(new { message = "Bạn không thể tự khóa tài khoản của chính mình." });
            }

            // Delegated checks cho DepartmentManager
            if (currentRole == "MANAGER")
            {
                var currentDbUser = await _context.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.UserId == currentUserId);
                var currentDeptId = currentDbUser?.DepartmentId;

                // Check 1: Thuộc tính DepartmentId của user bị khóa BẮT BUỘC phải trùng với CurrentUser.DepartmentId
                if (targetUser.DepartmentId != currentDeptId)
                {
                    return StatusCode(403, new { message = "Trưởng phòng chỉ được phép khóa/mở khóa nhân sự trong phòng ban của mình." });
                }

                // Check 2: Cấp quản lý mục tiêu không được là Admin hoặc Manager khác
                var targetRoles = targetUser.UserRoles.Select(ur => ur.Role.Code).ToList();
                if (targetRoles.Any(r => r == "ADMIN" || r == "MANAGER"))
                {
                    return StatusCode(403, new { message = "Trưởng phòng không có quyền can thiệp vào tài khoản cấp Quản lý khác hoặc Ban Giám Đốc." });
                }
            }

            // Đảo trạng thái Soft Delete (IsActive)
            targetUser.IsActive = !targetUser.IsActive;
            targetUser.UpdatedAt = DateTime.UtcNow;

            if (!targetUser.IsActive)
            {
                targetUser.LockedAt = DateTime.UtcNow;
                targetUser.LockedById = currentUserId;
                targetUser.LockedByName = adminName;
                targetUser.LockReason = request?.Reason ?? "Khóa tài khoản bởi cấp quản lý.";
            }
            else
            {
                targetUser.LockedAt = null;
                targetUser.LockedById = null;
                targetUser.LockedByName = null;
                targetUser.LockReason = null;
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                userId = targetUser.UserId,
                isActive = targetUser.IsActive,
                statusText = targetUser.IsActive ? "Đang hoạt động" : "Đã khóa",
                message = targetUser.IsActive ? "Mở khóa tài khoản thành công" : "Khóa tài khoản thành công"
            });
        }

        [HttpGet("{id}/pending-interviews")]
        [Authorize(Roles = "ADMIN,HR")]
        public async Task<IActionResult> GetPendingInterviews(Guid id)
        {
            var pendingInterviews = await _context.Interviews
                .Include(i => i.Application)
                    .ThenInclude(a => a.Candidate)
                .Include(i => i.Application)
                    .ThenInclude(a => a.Job)
                .Where(i => i.InterviewerId == id
                         && i.Status == "SCHEDULED"
                         && i.ScheduledStart > DateTime.UtcNow)
                .Select(i => new
                {
                    interviewId = i.InterviewId,
                    candidateName = i.Application.Candidate != null ? i.Application.Candidate.FullName : "N/A",
                    jobTitle = i.Application.Job != null ? i.Application.Job.Title : "N/A",
                    scheduledStart = i.ScheduledStart
                })
                .ToListAsync();

            return Ok(new
            {
                count = pendingInterviews.Count,
                interviews = pendingInterviews
            });
        }

        [HttpPut("{id}/deactivate")]
        [Authorize]
        public async Task<IActionResult> DeactivateEmployee(Guid id, [FromBody] DeactivateRequest? request = null)
        {
            return await ToggleStatus(id, request);
        }

        [HttpPut("{id}/reactivate")]
        [Authorize]
        public async Task<IActionResult> ReactivateEmployee(Guid id)
        {
            return await ToggleStatus(id, null);
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> UpdateEmployee(Guid id, [FromBody] CreateEmployeeRequest request)
        {
            // Check authorization
            var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? "";
            if (userRole != "ADMIN" && userRole != "MANAGER")
            {
                return StatusCode(403, new { message = " Bạn không có quyền chỉnh sửa thông tin nhân viên." });
            }
            
            var employee = await _employeeService.UpdateEmployeeAsync(id, request);
            
            if (employee == null)
            {
                return BadRequest(new { message = "Email đã tồn tại hoặc dữ liệu không hợp lệ" });
            }

            return Ok(employee);
        }
    }
}
