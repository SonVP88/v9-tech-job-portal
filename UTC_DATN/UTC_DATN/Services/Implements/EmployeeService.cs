using Microsoft.EntityFrameworkCore;
using UTC_DATN.Data;
using UTC_DATN.DTOs.Employee;
using UTC_DATN.Entities;
using UTC_DATN.Services.Interfaces;

namespace UTC_DATN.Services.Implements
{
    public class EmployeeService : IEmployeeService
    {
        private readonly UTC_DATNContext _context;

        public EmployeeService(UTC_DATNContext context)
        {
            _context = context;
        }

        public async Task<List<EmployeeDto>> GetEmployeesAsync()
        {
            var staffRoles = new[] { "ADMIN", "MANAGER", "HR", "INTERVIEWER" };

            var employees = await _context.Users
                .IgnoreQueryFilters()
                .Include(u => u.Department)
                .Include(u => u.Manager)
                .Include(u => u.Subordinates)
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .Where(u => u.UserRoles.Any(ur => staffRoles.Contains(ur.Role.Code)))
                .OrderBy(u => u.DepartmentId)
                .ThenBy(u => u.FullName)
                .Select(u => new EmployeeDto
                {
                    UserId = u.UserId,
                    FullName = u.FullName ?? string.Empty,
                    Email = u.Email,
                    Phone = u.Phone ?? string.Empty,
                    Role = u.UserRoles.Select(ur => ur.Role.Code).FirstOrDefault() ?? "HR",
                    RoleName = u.UserRoles.Select(ur => ur.Role.Code).FirstOrDefault() == "ADMIN" ? "Ban Giám Đốc" :
                               u.UserRoles.Select(ur => ur.Role.Code).FirstOrDefault() == "MANAGER" ? "Trưởng Phòng" :
                               u.UserRoles.Select(ur => ur.Role.Code).FirstOrDefault() == "HR" ? "Chuyên viên Nhân sự" :
                               u.UserRoles.Select(ur => ur.Role.Code).FirstOrDefault() == "INTERVIEWER" ? "Hội đồng Phỏng vấn" : "Nhân viên",
                    IsActive = u.IsActive,
                    CreatedAt = u.CreatedAt,
                    DepartmentId = u.DepartmentId,
                    DepartmentCode = u.Department != null ? u.Department.DepartmentCode : null,
                    DepartmentName = u.Department != null ? u.Department.DepartmentName : "Chưa phân bổ",
                    ManagerId = u.ManagerId,
                    ManagerName = u.Manager != null ? u.Manager.FullName : null,
                    SubordinatesCount = u.Subordinates.Count(),
                    LockedAt = u.LockedAt,
                    LockedByName = u.LockedByName,
                    LockReason = u.LockReason
                })
                .ToListAsync();

            return employees;
        }

        public async Task<EmployeeDto?> CreateEmployeeAsync(CreateEmployeeRequest request)
        {
            // Kiểm tra email đã tồn tại chưa (kể cả đã bị xóa mềm)
            var emailExists = await _context.Users.IgnoreQueryFilters().AnyAsync(u => u.Email == request.Email);
            if (emailExists)
            {
                return null;
            }

            var validRoles = new[] { "ADMIN", "MANAGER", "HR", "INTERVIEWER" };
            if (!validRoles.Contains(request.Role))
            {
                return null;
            }

            var role = await _context.Roles.FirstOrDefaultAsync(r => r.Code == request.Role);
            if (role == null)
            {
                return null;
            }

            var passwordHash = BCrypt.Net.BCrypt.HashPassword("123456");

            var newUser = new User
            {
                UserId = Guid.NewGuid(),
                Email = request.Email,
                NormalizedEmail = request.Email.ToUpper().Trim(),
                PasswordHash = passwordHash,
                FullName = request.FullName,
                Phone = request.PhoneNumber,
                DepartmentId = request.DepartmentId,
                ManagerId = request.ManagerId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(newUser);

            var userRole = new UserRole
            {
                UserId = newUser.UserId,
                RoleId = role.RoleId,
                CreatedAt = DateTime.UtcNow
            };

            _context.UserRoles.Add(userRole);
            await _context.SaveChangesAsync();

            // Load navigation props for DTO
            var dept = request.DepartmentId.HasValue 
                ? await _context.Departments.FindAsync(request.DepartmentId.Value) 
                : null;
            var mgr = request.ManagerId.HasValue 
                ? await _context.Users.FindAsync(request.ManagerId.Value) 
                : null;

            return new EmployeeDto
            {
                UserId = newUser.UserId,
                FullName = newUser.FullName,
                Email = newUser.Email,
                Phone = newUser.Phone,
                Role = request.Role,
                RoleName = request.Role == "ADMIN" ? "Ban Giám Đốc" :
                           request.Role == "MANAGER" ? "Trưởng Phòng" :
                           request.Role == "HR" ? "Chuyên viên Nhân sự" : "Hội đồng Phỏng vấn",
                IsActive = newUser.IsActive,
                CreatedAt = newUser.CreatedAt,
                DepartmentId = newUser.DepartmentId,
                DepartmentCode = dept?.DepartmentCode,
                DepartmentName = dept?.DepartmentName ?? "Chưa phân bổ",
                ManagerId = newUser.ManagerId,
                ManagerName = mgr?.FullName,
                SubordinatesCount = 0
            };
        }

        public async Task<bool> DeactivateEmployeeAsync(Guid userId, Guid adminId, string adminName, string? reason = null)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return false;

            user.IsActive = false;
            user.LockedAt = DateTime.UtcNow;
            user.LockedById = adminId;
            user.LockedByName = adminName;
            user.LockReason = reason;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ReactivateEmployeeAsync(Guid userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return false; 
            }

            user.IsActive = true;
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<EmployeeDto?> UpdateEmployeeAsync(Guid userId, CreateEmployeeRequest request)
        {
            var user = await _context.Users
                .Include(u => u.Subordinates)
                .FirstOrDefaultAsync(u => u.UserId == userId);
            if (user == null)
            {
                return null; 
            }

            var emailExists = await _context.Users.AnyAsync(u => u.Email == request.Email && u.UserId != userId);
            if (emailExists)
            {
                return null; 
            }

            var validRoles = new[] { "ADMIN", "MANAGER", "HR", "INTERVIEWER" };
            if (!validRoles.Contains(request.Role))
            {
                return null;
            }

            user.FullName = request.FullName;
            user.Email = request.Email;
            user.NormalizedEmail = request.Email.ToUpper().Trim();
            user.Phone = request.PhoneNumber;
            user.DepartmentId = request.DepartmentId;
            user.ManagerId = request.ManagerId;

            // Update role nếu thay đổi
            var currentUserRole = await _context.UserRoles
                .Include(ur => ur.Role)
                .FirstOrDefaultAsync(ur => ur.UserId == userId);

            if (currentUserRole != null && currentUserRole.Role.Code != request.Role)
            {
                _context.UserRoles.Remove(currentUserRole);

                var newRole = await _context.Roles.FirstOrDefaultAsync(r => r.Code == request.Role);
                if (newRole != null)
                {
                    var newUserRole = new UserRole
                    {
                        UserId = userId,
                        RoleId = newRole.RoleId,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.UserRoles.Add(newUserRole);
                }
            }
            else if (currentUserRole == null)
            {
                var newRole = await _context.Roles.FirstOrDefaultAsync(r => r.Code == request.Role);
                if (newRole != null)
                {
                    _context.UserRoles.Add(new UserRole
                    {
                        UserId = userId,
                        RoleId = newRole.RoleId,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            await _context.SaveChangesAsync();

            var dept = user.DepartmentId.HasValue 
                ? await _context.Departments.FindAsync(user.DepartmentId.Value) 
                : null;
            var mgr = user.ManagerId.HasValue 
                ? await _context.Users.FindAsync(user.ManagerId.Value) 
                : null;

            return new EmployeeDto
            {
                UserId = user.UserId,
                FullName = user.FullName,
                Email = user.Email,
                Phone = user.Phone ?? string.Empty,
                Role = request.Role,
                RoleName = request.Role == "ADMIN" ? "Ban Giám Đốc" :
                           request.Role == "MANAGER" ? "Trưởng Phòng" :
                           request.Role == "HR" ? "Chuyên viên Nhân sự" : "Hội đồng Phỏng vấn",
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                DepartmentId = user.DepartmentId,
                DepartmentCode = dept?.DepartmentCode,
                DepartmentName = dept?.DepartmentName ?? "Chưa phân bổ",
                ManagerId = user.ManagerId,
                ManagerName = mgr?.FullName,
                SubordinatesCount = user.Subordinates.Count()
            };
        }

        public async Task<List<DepartmentLookupDto>> GetDepartmentsAsync()
        {
            var depts = await _context.Departments
                .Select(d => new DepartmentLookupDto
                {
                    DepartmentId = d.DepartmentId,
                    DepartmentCode = d.DepartmentCode,
                    DepartmentName = d.DepartmentName,
                    EmployeeCount = _context.Users.Count(u => u.DepartmentId == d.DepartmentId)
                })
                .OrderBy(d => d.DepartmentCode)
                .ToListAsync();

            return depts;
        }

        public async Task<List<ManagerLookupDto>> GetManagersAsync()
        {
            var managerRoles = new[] { "ADMIN", "MANAGER" };
            var managers = await _context.Users
                .Include(u => u.Department)
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .Where(u => u.IsActive && u.UserRoles.Any(ur => managerRoles.Contains(ur.Role.Code)))
                .OrderBy(u => u.FullName)
                .Select(u => new ManagerLookupDto
                {
                    UserId = u.UserId,
                    FullName = u.FullName ?? string.Empty,
                    Email = u.Email,
                    Role = u.UserRoles.Select(ur => ur.Role.Code).FirstOrDefault() ?? "MANAGER",
                    DepartmentName = u.Department != null ? u.Department.DepartmentName : "Ban Giám Đốc"
                })
                .ToListAsync();

            return managers;
        }
    }
}
