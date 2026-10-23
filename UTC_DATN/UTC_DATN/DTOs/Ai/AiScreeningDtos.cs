namespace UTC_DATN.DTOs.Ai;

public class AiScreeningConfigDto
{
    public Guid JobId { get; set; }
    public int SkillsWeight { get; set; } = 40;
    public int ExperienceWeight { get; set; } = 30;
    public int HealthShiftWeight { get; set; } = 30;
}

public class CandidateScreeningResultDto
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string CandidateName { get; set; } = "Ứng viên";
    public string Email { get; set; } = "";
    public string Phone { get; set; } = "";
    public string FileName { get; set; } = "";
    public int MatchingScore { get; set; }
    public int SkillsScore { get; set; }
    public int ExperienceScore { get; set; }
    public int HealthShiftScore { get; set; }
    public List<string> ExtractedSkills { get; set; } = new();
    public string SummaryReview { get; set; } = "";
    public List<string> Strengths { get; set; } = new();
    public List<string> Weaknesses { get; set; } = new();
    public string SuggestedRole { get; set; } = "";
    public bool IsQualified { get; set; } = true;
    public string Status { get; set; } = "Analyzed";
}

public class ForwardToManagerRequestDto
{
    public List<string> CandidateIds { get; set; } = new();
    public Guid JobId { get; set; }
    public string DepartmentCode { get; set; } = "";
    public string Note { get; set; } = "";
}

public class ActiveCampaignDto
{
    public Guid JobId { get; set; }
    public string Title { get; set; } = "";
    public string Department { get; set; } = "";
    public string? DepartmentCode { get; set; }
    public int NumberOfPositions { get; set; }
    public string Location { get; set; } = "";
    public string Requirements { get; set; } = "";
}
