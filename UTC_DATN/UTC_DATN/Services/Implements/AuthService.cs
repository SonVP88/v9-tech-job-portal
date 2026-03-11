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
        private readonly IEmailService _emailService;

        public AuthService(UTC_DATNContext context, IConfiguration configuration, IEmailService emailService)
        {
            _context = context;
            _configuration = configuration;
            _emailService = emailService;
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
            
            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<string?> GoogleLoginAsync(string idToken)
        {
            var clientId = _configuration["GoogleAuth:ClientId"];

            // 1. Xác thực token với Google
            var settings = new Google.Apis.Auth.GoogleJsonWebSignature.ValidationSettings
            {
                Audience = new[] { clientId }
            };
            Google.Apis.Auth.GoogleJsonWebSignature.Payload payload;
            try
            {
                payload = await Google.Apis.Auth.GoogleJsonWebSignature.ValidateAsync(idToken, settings);
            }
            catch
            {
                return null; // Token không hợp lệ
            }

            // 2. Kiểm tra email đã được Google xác thực chưa
            if (!payload.EmailVerified)
                return null;

            var email = payload.Email;
            var normalizedEmail = email.ToUpper().Trim();

            // 3. Kiểm tra xem email có trong DB chưa
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail);

            if (existingUser != null)
            {
                // CHỐT CHẶN BẢO MẬT: Nếu tài khoản đã tồn tại và là Local → Từ chối
                if (existingUser.AuthProvider == "Local")
                {
                    throw new InvalidOperationException("EMAIL_REGISTERED_LOCALLY");
                }

                // Tài khoản đã là Google hoặc LocalAndGoogle → cho phép đăng nhập
                if (!existingUser.IsActive)
                    throw new InvalidOperationException("ACCOUNT_LOCKED");

                return await GenerateJwtToken(existingUser);
            }

            // 4. Email mới tinh → Tự động đăng ký
            var candidateRole = await _context.Roles
                .FirstOrDefaultAsync(r => r.Code == "CANDIDATE");
            if (candidateRole == null) return null;

            var newUser = new User
            {
                UserId = Guid.NewGuid(),
                Email = email,
                NormalizedEmail = normalizedEmail,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString()), // pass ngẫu nhiên, không dùng được
                FullName = payload.Name ?? email,
                AvatarUrl = payload.Picture,
                IsActive = true,
                AuthProvider = "Google",
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(newUser);

            var userRole = new UserRole
            {
                UserId = newUser.UserId,
                RoleId = candidateRole.RoleId,
                CreatedAt = DateTime.UtcNow
            };
            _context.UserRoles.Add(userRole);
            await _context.SaveChangesAsync();

            return await GenerateJwtToken(newUser);
        }

        public async Task<bool> LinkGoogleAsync(Guid userId, string idToken)
        {
            var clientId = _configuration["GoogleAuth:ClientId"];

            // 1. Xác thực token Google
            var settings = new Google.Apis.Auth.GoogleJsonWebSignature.ValidationSettings
            {
                Audience = new[] { clientId }
            };
            Google.Apis.Auth.GoogleJsonWebSignature.Payload payload;
            try
            {
                payload = await Google.Apis.Auth.GoogleJsonWebSignature.ValidateAsync(idToken, settings);
            }
            catch
            {
                return false;
            }

            // 2. Lấy user hiện tại từ DB
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return false;

            // 3. Email Google phải trùng khớp với email tài khoản hiện tại
            if (!string.Equals(user.Email, payload.Email, StringComparison.OrdinalIgnoreCase))
                return false;

            // 4. Cập nhật AuthProvider và Avatar (nếu chưa có)
            user.AuthProvider = "LocalAndGoogle";
            if (string.IsNullOrEmpty(user.AvatarUrl) && !string.IsNullOrEmpty(payload.Picture))
                user.AvatarUrl = payload.Picture;
            
            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> ForgotPasswordAsync(string email)
        {
            var normalizedEmail = email.Trim().ToUpper();
            var user = await _context.Users.FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail);

            if (user == null)
            {
                return false; // Email không tồn tại
            }

            // Sinh mật khẩu ngẫu nhiên
            var newPassword = GenerateRandomPassword(10);

            // Hash và lưu vào DB
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            // Gửi email
            var subject = "Khôi phục mật khẩu tài khoản V9 Tech";
            var body = $@"
                <h3>Xin chào {user.FullName},</h3>
                <p>Bạn đã yêu cầu khôi phục mật khẩu. Dưới đây là mật khẩu mới của bạn:</p>
                <h2 style='color: blue;'>{newPassword}</h2>
                <p>Vui lòng đăng nhập bằng mật khẩu này và đổi lại mật khẩu ngay lập tức tại phần Cài đặt tài khoản (nếu muốn) hoặc liên kết với tài khoản Google để đăng nhập thuận tiện hơn.</p>
                <br>
                <p>Trân trọng,<br>Đội ngũ hỗ trợ V9 Tech</p>
            ";

            await _emailService.SendEmailAsync(user.Email, subject, body);

            return true;
        }

        private string GenerateRandomPassword(int length)
        {
            var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}
