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
            // Query users có role HR hoặc INTERVIEWER
            var employees = await _context.Users
                .Join(_context.UserRoles,
                    user => user.UserId,
                    userRole => userRole.UserId,
                    (user, userRole) => new { user, userRole })
                .Join(_context.Roles,
                    combined => combined.userRole.RoleId,
                    role => role.RoleId,
                    (combined, role) => new { combined.user, role })
                .Where(x => x.role.Code == "HR" || x.role.Code == "INTERVIEWER")
                .Select(x => new EmployeeDto
                {
                    UserId = x.user.UserId,
                    FullName = x.user.FullName,
                    Email = x.user.Email,
                    Phone = x.user.Phone,
                    Role = x.role.Code,
                    IsActive = x.user.IsActive,
                    CreatedAt = x.user.CreatedAt,
                    // Audit Trail
                    LockedAt = x.user.LockedAt,
                    LockedByName = x.user.LockedByName,
                    LockReason = x.user.LockReason
                })
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync();

            return employees;
        }

        public async Task<EmployeeDto?> CreateEmployeeAsync(CreateEmployeeRequest request)
        {
            // Kiểm tra email đã tồn tại chưa
            var emailExists = await _context.Users.AnyAsync(u => u.Email == request.Email);
            if (emailExists)
            {
                return null; // Email đã tồn tại
            }

            // Validate role phải là HR hoặc INTERVIEWER
            if (request.Role != "HR" && request.Role != "INTERVIEWER")
            {
                return null; // Role không hợp lệ
            }

            // Tìm role theo Code
            var role = await _context.Roles.FirstOrDefaultAsync(r => r.Code == request.Role);
            if (role == null)
            {
                return null; // Role không tồn tại trong database
            }

            // Hash password mặc định
            var passwordHash = BCrypt.Net.BCrypt.HashPassword("123456");

            // Tạo user mới
            var newUser = new User
            {
                UserId = Guid.NewGuid(),
                Email = request.Email,
                NormalizedEmail = request.Email.ToUpper().Trim(),
                PasswordHash = passwordHash,
                FullName = request.FullName,
                Phone = request.PhoneNumber,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(newUser);

            // Gán role cho user
            var userRole = new UserRole
            {
                UserId = newUser.UserId,
                RoleId = role.RoleId,
                CreatedAt = DateTime.UtcNow
            };

            _context.UserRoles.Add(userRole);

            // Lưu vào database
            await _context.SaveChangesAsync();

            // Trả về EmployeeDto
            return new EmployeeDto
            {
                UserId = newUser.UserId,
                FullName = newUser.FullName,
                Email = newUser.Email,
                Phone = newUser.Phone,
                Role = request.Role,
                IsActive = newUser.IsActive,
                CreatedAt = newUser.CreatedAt
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
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return null; 
            }

            // Check email trùng (ngoại trừ user hiện tại)
            var emailExists = await _context.Users.AnyAsync(u => u.Email == request.Email && u.UserId != userId);
            if (emailExists)
            {
                return null; 
            }

            // Validate role
            if (request.Role != "HR" && request.Role != "INTERVIEWER")
            {
                return null;
            }

            // Update user info
            user.FullName = request.FullName;
            user.Email = request.Email;
            user.NormalizedEmail = request.Email.ToUpper().Trim();
            user.Phone = request.PhoneNumber;

            // Update role nếu thay đổi
            var currentUserRole = await _context.UserRoles
                .Include(ur => ur.Role)
                .FirstOrDefaultAsync(ur => ur.UserId == userId);

            if (currentUserRole != null && currentUserRole.Role.Code != request.Role)
            {
                _context.UserRoles.Remove(currentUserRole);

                // Thêm role mới
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

            await _context.SaveChangesAsync();

            return new EmployeeDto
            {
                UserId = user.UserId,
                FullName = user.FullName,
                Email = user.Email,
                Phone = user.Phone,
                Role = request.Role,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt
            };
        }
    }
}
