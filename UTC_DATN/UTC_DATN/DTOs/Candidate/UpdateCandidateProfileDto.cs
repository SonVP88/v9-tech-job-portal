using System.ComponentModel.DataAnnotations;

namespace UTC_DATN.DTOs.Candidate
{
    public class UpdateCandidateProfileDto
    {
        [Required(ErrorMessage = "Họ tên là bắt buộc")]
        [StringLength(200, ErrorMessage = "Họ tên không được vượt quá 200 ký tự")]
        public string FullName { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        public string? Phone { get; set; }

        [StringLength(300, ErrorMessage = "Địa chỉ không được vượt quá 300 ký tự")]
        public string? Location { get; set; }

        [StringLength(200, ErrorMessage = "Tiêu đề không được vượt quá 200 ký tự")]
        public string? Headline { get; set; }

        [StringLength(2000, ErrorMessage = "Tóm tắt không được vượt quá 2000 ký tự")]
        public string? Summary { get; set; }

        public string? LinkedIn { get; set; }

        public string? GitHub { get; set; }

        public string? Avatar { get; set; }

        public List<Guid> SkillIds { get; set; } = new();
        
        public List<string> Skills { get; set; } = new();
    }
}
