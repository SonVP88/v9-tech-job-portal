namespace UTC_DATN.DTOs.Recommendation;

public class RecommendedJobDto
{
    public Guid JobId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string? Location { get; set; }
    public decimal? SalaryMin { get; set; }
    public decimal? SalaryMax { get; set; }
    public string? EmploymentType { get; set; }
    public DateTime? Deadline { get; set; }
    public DateTime CreatedDate { get; set; }
    public List<string> Skills { get; set; } = new List<string>();
    
    // Thuộc tính AI Recommendation
    public double MatchScore { get; set; } // Điểm phù hợp %
    public int MatchedSkillsCount { get; set; } // Số lượng kỹ năng trùng khớp
    public int TotalRequiredSkills { get; set; } // Tổng số kỹ năng yêu cầu
}
