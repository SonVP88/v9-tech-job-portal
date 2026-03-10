namespace UTC_DATN.DTOs.Recommendation;

public class RecommendedCandidateDto
{
    public Guid CandidateId { get; set; }
    public Guid? UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string? AvatarUrl { get; set; }
    public int YearsOfExperience { get; set; }
    public List<string> Skills { get; set; } = new List<string>();
    
    // Thuộc tính AI Recommendation
    public double MatchScore { get; set; } // Điểm phù hợp %
    public int MatchedSkillsCount { get; set; }
    public int TotalRequiredSkills { get; set; }
}
