using System.ComponentModel.DataAnnotations;

namespace UTC_DATN.DTOs.Job;

public class CreateJobRequest : IValidatableObject
{
    [Required(ErrorMessage = "Tên chức danh là bắt buộc")]
    [StringLength(200, MinimumLength = 10, ErrorMessage = "Tên chức danh phải từ 10 đến 200 ký tự")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Phòng ban / Phân xưởng tuyển dụng (PROD_LINE, QA_QC, WH_LOGS...)
    /// </summary>
    public Guid? DepartmentId { get; set; }

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

    [Required(ErrorMessage = "Mức kinh nghiệm là bắt buộc")]
    [StringLength(100)]
    public string ExperienceLevel { get; set; } = string.Empty;

    [StringLength(100)]
    public string? SeniorityLevel { get; set; }

    [Required(ErrorMessage = "Hạn nộp hồ sơ là bắt buộc")]
    public DateTime? Deadline { get; set; }

    [Required]
    [MinLength(1, ErrorMessage = "Phải chọn ít nhất 1 kỹ năng")]
    public List<Guid> SkillIds { get; set; } = new List<Guid>();

    /// <summary>
    /// Tự tạo Google Form + mã QR khi đăng tin (mặc định bật)
    /// </summary>
    public bool CreateCampaignQr { get; set; } = true;

    /// <summary>Luồng tuyển dụng: 'PROFESSIONAL' | 'MASS_LABOR'</summary>
    public string RecruitmentTrack { get; set; } = "PROFESSIONAL";

    /// <summary>Chỉ tiêu cần tuyển</summary>
    public int TargetQuantity { get; set; } = 50;

    /// <summary>Bắt buộc làm ca đêm (22h-06h) hoặc nặng nhọc -> chặn dưới 18 tuổi</summary>
    public bool RequireNightShift { get; set; } = false;

    /// <summary>Lý do đề xuất tuyển dụng (cho Quản đốc xưởng)</summary>
    public string? RequisitionNote { get; set; }

    /// <summary>Trạng thái định biên yêu cầu (Pending_Approval nếu Quản đốc xưởng)</summary>
    public string? RequisitionStatus { get; set; }

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
