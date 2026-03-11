using UTC_DATN.DTOs.Auth;

namespace UTC_DATN.Services.Interfaces
{
    public interface IAuthService
    {
        /// <summary>
        /// Đăng ký tài khoản mới với role CANDIDATE
        /// </summary>
        /// <param name="request">Thông tin đăng ký</param>
        /// <returns>True nếu đăng ký thành công, False nếu thất bại</returns>
        Task<bool> RegisterAsync(RegisterRequest request);

        /// <summary>
        /// Đăng nhập và trả về JWT token
        /// </summary>
        /// <param name="request">Thông tin đăng nhập</param>
        /// <returns>JWT token string nếu thành công, null nếu thất bại</returns>
        Task<string?> LoginAsync(LoginRequest request);
        Task<bool> ChangePasswordAsync(Guid userId, ChangePasswordDto request); // New method

        /// <summary>
        /// Xử lý Quên mật khẩu: Sinh mật khẩu mới và gửi qua Email
        /// </summary>
        /// <param name="email">Email của người dùng</param>
        /// <returns>True nếu thành công, False nếu email không tồn tại</returns>
        Task<bool> ForgotPasswordAsync(string email);

        /// <summary>
        /// Đăng nhập / Đăng ký qua Google OAuth2 (Strict No-Merge)
        /// </summary>
        Task<string?> GoogleLoginAsync(string idToken);

        /// <summary>
        /// Liên kết tài khoản Google với tài khoản Local hiện tại
        /// </summary>
        Task<bool> LinkGoogleAsync(Guid userId, string idToken);
    }
}
