using Microsoft.AspNetCore.Mvc;
using UTC_DATN.DTOs.Auth;
using UTC_DATN.Services.Interfaces;

namespace UTC_DATN.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        /// <summary>
        /// Đăng ký tài khoản mới
        /// </summary>
        /// <param name="request">Thông tin đăng ký</param>
        /// <returns>Kết quả đăng ký</returns>
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _authService.RegisterAsync(request);

            if (result)
            {
                return Ok("User registered successfully");
            }

            return BadRequest(new { message = "Email này đã được đăng ký trong hệ thống." });
        }

        /// <summary>
        /// Đăng nhập và nhận JWT token
        /// </summary>
        /// <param name="request">Thông tin đăng nhập</param>
        /// <returns>JWT token nếu thành công</returns>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var result = await _authService.LoginAsync(request);

            if (result == null)
            {
                return Unauthorized("Invalid credentials");
            }

            return Ok(new { Token = result });
        }

        [HttpPost("change-password")]
        [Microsoft.AspNetCore.Authorization.Authorize] // Require login
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto request)
        {
            var userIdClaim = User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);
            
            if (userIdClaim == null)
            {
                userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            }

            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out Guid userId))
            {
                return Unauthorized("Không xác thực được người dùng.");
            }

            var result = await _authService.ChangePasswordAsync(userId, request);
            if (!result)
            {
                return BadRequest("Mật khẩu hiện tại không chính xác hoặc có lỗi xảy ra.");
            }

            return Ok(new { message = "Đổi mật khẩu thành công!" });
        }
    }
}
