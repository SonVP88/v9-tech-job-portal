using System.ComponentModel.DataAnnotations;

namespace UTC_DATN.DTOs.Employee
{
    public class CreateEmployeeRequest
    {
        [Required(ErrorMessage = "Họ tên là bắt buộc")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email là bắt buộc")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Số điện thoại là bắt buộc")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Role là bắt buộc")]
        [RegularExpression("^(ADMIN|MANAGER|HR|INTERVIEWER)$", ErrorMessage = "Role chỉ có thể là 'ADMIN', 'MANAGER', 'HR' hoặc 'INTERVIEWER'")]
        public string Role { get; set; } = string.Empty;

        public Guid? DepartmentId { get; set; }
        public Guid? ManagerId { get; set; }
    }
}
