using System.ComponentModel.DataAnnotations;

namespace UTC_DATN.DTOs.Auth
{
    public class ForgotPasswordDto
    {
        [Required(ErrorMessage = "Vui lòng nhập email")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; } = string.Empty;
    }
}
