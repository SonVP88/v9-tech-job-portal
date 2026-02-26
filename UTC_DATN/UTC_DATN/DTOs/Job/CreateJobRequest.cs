using System.ComponentModel.DataAnnotations;

namespace UTC_DATN.DTOs.Job;

public class CreateJobRequest : IValidatableObject
{
    [Required(ErrorMessage = "Tên chức danh là bắt buộc")]
    [StringLength(200, MinimumLength = 10, ErrorMessage = "Tên chức danh phải từ 10 đến 200 ký tự")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mô tả công việc là bắt buộc")]
    [StringLength(4000, MinimumLength = 50, ErrorMessage = "Mô tả công việc phải từ 50 đến 4000 ký tự")]
    public string Description { get; set; } = string.Empty;

    [StringLength(4000)]
    public string? Requirements { get; set; }

    [StringLength(4000)]
    public string? Benefits { get; set; }

    [Range(1, 1000, ErrorMessage = "Số lượng tuyển tối thiểu là 1")]
    public int? NumberOfPositions { get; set; }

    [Range(0, 1000000000, ErrorMessage = "Mức lương không hợp lệ")]
    public decimal? SalaryMin { get; set; }

    [Range(0, 1000000000, ErrorMessage = "Mức lương không hợp lệ")]
    public decimal? SalaryMax { get; set; }

    [Required(ErrorMessage = "Địa điểm là bắt buộc")]
    [StringLength(500)]
    public string Location { get; set; } = string.Empty;

    [Required(ErrorMessage = "Loại hình công việc là bắt buộc")]
    public string EmploymentType { get; set; } = string.Empty;

    [Required(ErrorMessage = "Hạn nộp hồ sơ là bắt buộc")]
    public DateTime? Deadline { get; set; }

    [Required]
    [MinLength(1, ErrorMessage = "Phải chọn ít nhất 1 kỹ năng")]
    public List<Guid> SkillIds { get; set; } = new List<Guid>();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (SalaryMin.HasValue && SalaryMax.HasValue && SalaryMax.Value < SalaryMin.Value)
        {
            yield return new ValidationResult("Mức lương tối đa phải lớn hơn hoặc bằng mức lương tối thiểu.", new[] { nameof(SalaryMax) });
        }

        if (Deadline.HasValue && Deadline.Value.Date <= DateTime.UtcNow.Date)
        {
            yield return new ValidationResult("Hạn nộp hồ sơ phải từ ngày mai trở đi.", new[] { nameof(Deadline) });
        }
    }
}
