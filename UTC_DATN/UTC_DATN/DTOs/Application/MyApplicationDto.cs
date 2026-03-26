namespace UTC_DATN.DTOs.Application;

public class MyApplicationDto
{
    public Guid ApplicationId { get; set; }
    public Guid JobId { get; set; }
    public string JobTitle { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string? JobLocation { get; set; }
    public DateTime AppliedAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? CvUrl { get; set; }
    public DateTime? LastViewedAt { get; set; }
}
