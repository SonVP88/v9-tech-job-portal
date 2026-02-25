using System.ComponentModel.DataAnnotations;

namespace UTC_DATN.DTOs.Job;

public class UpdateJobRequest
{
    [Required(ErrorMessage = "Title không được để trống")]
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string? Requirements { get; set; }

    public string? Benefits { get; set; }

    public int? NumberOfPositions { get; set; }

    public decimal? SalaryMin { get; set; }

    public decimal? SalaryMax { get; set; }

    public string? Location { get; set; }

    public string? EmploymentType { get; set; }

    public DateTime? Deadline { get; set; }

    public List<Guid>? SkillIds { get; set; }
}
