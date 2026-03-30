namespace UTC_DATN.DTOs.Application;

public class SlaDashboardDto
{
    public int TotalTrackedApplications { get; set; }
    public int OnTrackApplications { get; set; }
    public int OverdueApplications { get; set; }
    public int WarningApplications { get; set; }
    public int SevereOverdueApplications { get; set; }
    public double ComplianceRate { get; set; }
    public double SlaHealthScore { get; set; }
    public List<SlaRecruiterBottleneckDto> Recruiters { get; set; } = new();
    public List<SlaStageBottleneckDto> Stages { get; set; } = new();
    public List<SlaStuckApplicationDto> TopStuckApplications { get; set; } = new();
}

public class SlaRecruiterBottleneckDto
{
    public Guid? RecruiterId { get; set; }
    public string RecruiterName { get; set; } = string.Empty;
    public int TotalApplications { get; set; }
    public int OnTrackApplications { get; set; }
    public int OverdueApplications { get; set; }
    public int WarningApplications { get; set; }
    public int SevereOverdueApplications { get; set; }
    public double ComplianceRate { get; set; }
    public double SlaHealthScore { get; set; }
    public string RiskLevel { get; set; } = "LOW";
    public int MaxOverdueDays { get; set; }
    public double AvgOverdueDays { get; set; }
}

public class SlaStageBottleneckDto
{
    public string StageName { get; set; } = string.Empty;
    public int TotalApplications { get; set; }
    public int OnTrackApplications { get; set; }
    public int OverdueApplications { get; set; }
    public int WarningApplications { get; set; }
    public int SevereOverdueApplications { get; set; }
    public double ComplianceRate { get; set; }
    public string RiskLevel { get; set; } = "LOW";
    public int MaxOverdueDays { get; set; }
    public double AvgOverdueDays { get; set; }
}

public class SlaStuckApplicationDto
{
    public Guid ApplicationId { get; set; }
    public string CandidateName { get; set; } = string.Empty;
    public string JobTitle { get; set; } = string.Empty;
    public string StageName { get; set; } = string.Empty;
    public string RecruiterName { get; set; } = string.Empty;
    public DateTime EnteredStageAt { get; set; }
    public DateTime DueAt { get; set; }
    public int OverdueDays { get; set; }
}
