namespace UTC_DATN.DTOs.Application;

public class ApplicationDto
{
    public Guid ApplicationId { get; set; }
    public Guid CandidateId { get; set; }  // Thêm để gọi API generate-opening
    public string CandidateName { get; set; }
    public string Email { get; set; }
    public string Phone { get; set; }
    public DateTime AppliedAt { get; set; }
    public string CvUrl { get; set; }
    public string Status { get; set; }
    public string? JobTitle { get; set; }  // Thêm để hiển thị trong email
    public Guid JobId { get; set; } // Thêm để xác định job khi view all

    // Thông tin AI Scoring
    public int? MatchScore { get; set; }
    public string? AiExplanation { get; set; }
}

