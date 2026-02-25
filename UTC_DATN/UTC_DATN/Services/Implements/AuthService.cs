using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using UTC_DATN.Data;
using UTC_DATN.DTOs.Auth;
using UTC_DATN.Entities;
using UTC_DATN.Services.Interfaces;

namespace UTC_DATN.Services.Implements
{
    public class AuthService : IAuthService
    {
        private readonly UTC_DATNContext _context;
        private readonly IConfiguration _configuration;

        public AuthService(UTC_DATNContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public async Task<bool> RegisterAsync(RegisterRequest request)
        {
            // Kiểm tra email đã tồn tại chưa
            var emailExists = await _context.Users.AnyAsync(u => u.Email == request.Email);
            if (emailExists)
            {
                return false;
            }

            // Hash mật khẩu bằng BCrypt
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            var newUser = new User
            {
                UserId = Guid.NewGuid(),
                Email = request.Email,
                NormalizedEmail = request.Email.ToUpper().Trim(),
                PasswordHash = passwordHash,
                FullName = request.FullName,
                Phone = request.Phone,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            // Tìm Role có Code là 'CANDIDATE'
            var candidateRole = await _context.Roles
                .FirstOrDefaultAsync(r => r.Code == "CANDIDATE");

            if (candidateRole == null)
            {
                return false; 
            }

            _context.Users.Add(newUser);

            // Tạo UserRole để gán role cho user
            var userRole = new UserRole
            {
                UserId = newUser.UserId,
                RoleId = candidateRole.RoleId,
                CreatedAt = DateTime.UtcNow
            };

            _context.UserRoles.Add(userRole);

            // Lưu vào database
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<string?> LoginAsync(LoginRequest request)
        {
            // Tìm User theo Email
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email);

            if (user == null)
            {
                return null; // User không tồn tại
            }

            // Kiểm tra password bằng BCrypt
            var isPasswordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);
            if (!isPasswordValid)
            {
                return null; // Mật khẩu sai
            }

            // Kiểm tra user có active không
            if (!user.IsActive)
            {
                return null; // User bị vô hiệu hóa
            }

            // Tạo và trả về JWT token
            var token = await GenerateJwtToken(user);
            return token;
        }

        private async Task<string> GenerateJwtToken(User user)
        {
            // Đọc cấu hình JWT từ appsettings.json
            var key = _configuration["JwtSettings:Key"];
            var issuer = _configuration["JwtSettings:Issuer"];
            var audience = _configuration["JwtSettings:Audience"];
            var durationInMinutes = int.Parse(_configuration["JwtSettings:DurationInMinutes"] ?? "60");

            // Lấy danh sách roles của user
            var userRoles = await _context.UserRoles
                .Where(ur => ur.UserId == user.UserId)
                .Include(ur => ur.Role)
                .Select(ur => ur.Role.Code)
                .ToListAsync();

            // Tạo Claims
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim("FullName", user.FullName)
            };

            // Thêm claims cho từng role
            foreach (var role in userRoles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            // Tạo Security Key
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key!));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            // Tạo JWT Token
            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(durationInMinutes),
                signingCredentials: credentials
            );

            // Trả về chuỗi token
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public async Task<bool> ChangePasswordAsync(Guid userId, ChangePasswordDto request)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return false;

            // Verify old password
            if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
            {
                return false;
            }

            // Hash new password
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            
            // In a real scenario, we might increment a "TokenVersion" field here to invalidate old tokens
            // user.TokenVersion++; 

            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            return true;
        }
    }
}
