using System;

namespace UTC_DATN.DTOs.Interview;

/// <summary>
/// DTO trả về danh sách lịch phỏng vấn của người phỏng vấn
/// </summary>
public class MyInterviewDto
{
    public Guid InterviewId { get; set; }
    public Guid InterviewerId { get; set; }
    
    public string CandidateName { get; set; } = string.Empty;
    
    public string JobTitle { get; set; } = string.Empty;
    
    public string Position { get; set; } = string.Empty;
    
    public DateTime InterviewTime { get; set; }
    
    public string FormattedTime { get; set; } = string.Empty; // "10:00 AM"
    
    public string FormattedDate { get; set; } = string.Empty; // "15/01/2026"
    
    public string? Location { get; set; }
    
    public string? MeetingLink { get; set; }
    
    public string LocationType { get; set; } = string.Empty; // "online" | "offline"
    
    public string Status { get; set; } = string.Empty; // "upcoming" | "ongoing" | "completed"
    
    public string? CandidateEmail { get; set; }
    
    public string? CandidatePhone { get; set; }
    public string InterviewerName { get; set; } = string.Empty;
    
    public string? InterviewerEmail { get; set; }
}
