namespace UTC_DATN.DTOs.Job;

/// <summary>
/// DTO để hiển thị chi tiết job
/// </summary>
public class JobDetailDto
{
    public Guid JobId { get; set; }
    
    public string Title { get; set; } = string.Empty;
    
    public string CompanyName { get; set; } = string.Empty; // Lấy từ User.FullName
    
    public decimal? SalaryMin { get; set; }
    
    public decimal? SalaryMax { get; set; }
    
    public string? Location { get; set; }
    
    public string? EmploymentType { get; set; }

    public string Status { get; set; }
    public DateTime? Deadline { get; set; }
    public DateTime CreatedAt { get; set; }
 // Map từ Job.ClosedAt
    
    public DateTime CreatedDate { get; set; } // Map từ Job.CreatedAt
    
    public List<string> Skills { get; set; } = new List<string>(); // Danh sách tên skills
    
    public List<Guid> SkillIds { get; set; } = new List<Guid>(); // Danh sách ID skills (cho việc update)
    
    // Thông tin chi tiết thêm
    public string? Description { get; set; } // Mô tả công việc đầy đủ
    
    public string? Requirements { get; set; } // Yêu cầu công việc
    
    public string? Benefits { get; set; } // Quyền lợi
    
    public string? ContactEmail { get; set; } // Email liên hệ (từ User)
    
    public int? NumberOfPositions { get; set; } // Số lượng tuyển
}
