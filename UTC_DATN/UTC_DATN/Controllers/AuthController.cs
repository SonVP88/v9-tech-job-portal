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
                return Ok(new { message = "Đăng ký tài khoản thành công!" });
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
                return Unauthorized(new { message = "Thông tin đăng nhập không chính xác" });
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
                return Unauthorized(new { message = "Không xác thực được người dùng." });
            }

            var result = await _authService.ChangePasswordAsync(userId, request);
            if (!result)
            {
                return BadRequest(new { message = "Mật khẩu hiện tại không chính xác hoặc có lỗi xảy ra." });
            }

            return Ok(new { message = "Đổi mật khẩu thành công!" });
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _authService.ForgotPasswordAsync(request.Email);

            if (!result)
            {
                return Ok(new { message = "Nếu email này tồn tại trong hệ thống, mật khẩu mới đã được gửi đến hòm thư của bạn. Vui lòng kiểm tra." });
            }

            return Ok(new { message = "Nếu email này tồn tại trong hệ thống, mật khẩu mới đã được gửi đến hòm thư của bạn. Vui lòng kiểm tra." });
        }

        [HttpPost("google-login")]
        public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginRequestDto request)
        {
            try
            {
                var token = await _authService.GoogleLoginAsync(request.IdToken);
                if (token == null)
                    return Unauthorized(new { message = "Xác thực Google thất bại. Vui lòng thử lại." });

                return Ok(new { Token = token });
            }
            catch (InvalidOperationException ex) when (ex.Message == "EMAIL_REGISTERED_LOCALLY")
            {
                return BadRequest(new
                {
                    message = "Email này đã được đăng ký bằng mật khẩu. Hệ thống không cho phép đăng nhập bằng Google để bảo vệ an toàn. Vui lòng đăng nhập bằng Mật khẩu hoặc dùng tính năng \"Quên mật khẩu\" để lấy lại tài khoản.",
                    errorCode = "EMAIL_REGISTERED_LOCALLY"
                });
            }
            catch (InvalidOperationException ex) when (ex.Message == "ACCOUNT_LOCKED")
            {
                return BadRequest(new { message = "Tài khoản của bạn đã bị vô hiệu hóa. Vui lòng liên hệ hỗ trợ.", errorCode = "ACCOUNT_LOCKED" });
            }
        }

        [HttpPost("link-google")]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public async Task<IActionResult> LinkGoogle([FromBody] GoogleLoginRequestDto request)
        {
            var userIdClaim = User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)
                            ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);

            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out Guid userId))
                return Unauthorized(new { message = "Không xác thực được người dùng." });

            var result = await _authService.LinkGoogleAsync(userId, request.IdToken);

            if (!result)
                return BadRequest(new { message = "Liên kết thất bại. Tài khoản Google không trùng với email đăng nhập hiện tại." });

            return Ok(new { message = "Liên kết tài khoản Google thành công! Từ nay bạn có thể đăng nhập bằng cả 2 cách." });
        }
    }
}
