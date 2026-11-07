namespace UTC_DATN.DTOs.Job;

/// <summary>
/// DTO để hiển thị job trên trang chủ
/// </summary>
public class JobHomeDto
{
    public Guid JobId { get; set; }
    
    public string Title { get; set; } = string.Empty;
    
    public string CompanyName { get; set; } = string.Empty; 

    public Guid? DepartmentId { get; set; }

    public string? DepartmentName { get; set; }
    
    public string? CreatedByName { get; set; } 
    
    public string? CreatedByRole { get; set; } 
    
    public decimal? SalaryMin { get; set; }
    
    public decimal? SalaryMax { get; set; }
    
    public string? Location { get; set; }
    
    public string? EmploymentType { get; set; }

    public string? ExperienceLevel { get; set; }

    public string? SeniorityLevel { get; set; }
    
    public DateTime? Deadline { get; set; } // Map từ Job.ClosedAt
    
    public DateTime CreatedDate { get; set; } // Map từ Job.CreatedAt
    public string Status { get; set; } = string.Empty; // OPEN or CLOSED

    public int? NumberOfPositions { get; set; } // Số lượng cần tuyển
    
    public int TotalHired { get; set; } // Số lượng ứng viên đã gửi offer và xác nhận (HIRED)

    public List<string> Skills { get; set; } = new List<string>(); // Danh sách tên skills

    // Audit Trail
    public string? ClosedByName { get; set; } // Ai đã đóng job này?
    public DateTime? ClosedAt { get; set; } // Khi nào job này bị đóng?

    // Candidate Application Status (chỉ populate khi user đã đăng nhập)
    public bool HasApplied { get; set; } = false; // Có phải đã ứng tuyển công việc này không?
    public DateTime? AppliedAt { get; set; } // Thời gian ứng tuyển (nếu đã ứng tuyển)

    public string? CampaignFormSource { get; set; }
    public string? GoogleFormPublicUrl { get; set; }
    public string? QrCodeImagePath { get; set; }
}
