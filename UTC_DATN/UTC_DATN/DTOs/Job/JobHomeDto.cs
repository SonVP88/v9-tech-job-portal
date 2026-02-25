namespace UTC_DATN.DTOs.Job;

/// <summary>
/// DTO để hiển thị job trên trang chủ
/// </summary>
public class JobHomeDto
{
    public Guid JobId { get; set; }
    
    public string Title { get; set; } = string.Empty;
    
    public string CompanyName { get; set; } = string.Empty; 
    
    public string? CreatedByName { get; set; } 
    
    public string? CreatedByRole { get; set; } 
    
    public decimal? SalaryMin { get; set; }
    
    public decimal? SalaryMax { get; set; }
    
    public string? Location { get; set; }
    
    public string? EmploymentType { get; set; }
    
    public DateTime? Deadline { get; set; } // Map từ Job.ClosedAt
    
    public DateTime CreatedDate { get; set; } // Map từ Job.CreatedAt
    
    public string Status { get; set; } = string.Empty; // OPEN or CLOSED

    public List<string> Skills { get; set; } = new List<string>(); // Danh sách tên skills
}
