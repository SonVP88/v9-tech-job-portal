using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using UTC_DATN.DTOs.Employee;
using UTC_DATN.Services.Interfaces;

namespace UTC_DATN.Controllers
{
    [ApiController]
    [Route("api/employees")]
    public class EmployeeController : ControllerBase
    {
        private readonly IEmployeeService _employeeService;

        public EmployeeController(IEmployeeService employeeService)
        {
            _employeeService = employeeService;
        }

        /// <summary>
        /// Lấy danh sách nhân viên (HR và INTERVIEWER)
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> GetEmployees()
        {
            var employees = await _employeeService.GetEmployeesAsync();
            return Ok(employees);
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
        /// Tạo nhân viên mới (HR hoặc INTERVIEWER)
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> CreateEmployee([FromBody] CreateEmployeeRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var employee = await _employeeService.CreateEmployeeAsync(request);

            if (employee == null)
            {
                return BadRequest(new { message = "Có lỗi xảy ra khi tạo nhân viên" });
            }

            return CreatedAtAction(nameof(GetEmployees), new { id = employee.UserId }, employee);
        }

        [HttpPut("{id}/deactivate")]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> DeactivateEmployee(Guid id, [FromBody] DeactivateRequest? request = null)
        {
            if (!Guid.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out Guid adminId))
                return Unauthorized();
            var fullName = User.FindFirst("FullName")?.Value ?? "User";
            var role = User.FindFirst(ClaimTypes.Role)?.Value ?? "Admin";
            var adminName = $"{fullName} {role}";

            var result = await _employeeService.DeactivateEmployeeAsync(id, adminId, adminName, request?.Reason);
            
            if (!result)
                return NotFound(new { message = "Không tìm thấy nhân viên" });

            return Ok(new { message = "Vô hiệu hóa nhân viên thành công" });
        }

        [HttpPut("{id}/reactivate")]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> ReactivateEmployee(Guid id)
        {
            var result = await _employeeService.ReactivateEmployeeAsync(id);
            
            if (!result)
            {
                return NotFound(new { message = "Không tìm thấy nhân viên" });
            }

            return Ok(new { message = "Kích hoạt nhân viên thành công" });
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> UpdateEmployee(Guid id, [FromBody] CreateEmployeeRequest request)
        {
            var employee = await _employeeService.UpdateEmployeeAsync(id, request);
            
            if (employee == null)
            {
                return BadRequest(new { message = "Email đã tồn tại hoặc dữ liệu không hợp lệ" });
            }

            return Ok(employee);
        }
    }
}
