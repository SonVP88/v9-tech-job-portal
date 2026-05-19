namespace UTC_DATN.DTOs.Sla;

/// <summary>
/// DTO cho SLA stage configuration
/// </summary>
public class SlaStageConfigDto
{
    public Guid SlaStageConfigId { get; set; }
    public string StageCode { get; set; }
    public string StageName { get; set; }
    public int SlaMaxDays { get; set; }
    public int SlaWarnBeforeDays { get; set; }
    public bool IsActive { get; set; }
    public int OrderSequence { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// DTO cho upsert SLA configuration
/// </summary>
public class UpsertSlaStageConfigDto
{
    public string StageCode { get; set; }
    public string StageName { get; set; }
    public int SlaMaxDays { get; set; }
    public int SlaWarnBeforeDays { get; set; }
    public bool IsActive { get; set; }
    public int OrderSequence { get; set; }
}

/// <summary>
/// DTO cho SLA tracking của một application
/// </summary>
public class ApplicationSlaTrackingDto
{
    public Guid SlaTrackingId { get; set; }
    public Guid ApplicationId { get; set; }
    public string CurrentStageCode { get; set; }
    public string CurrentStageName { get; set; }
    public DateTime EnteredStageAt { get; set; }
    public int SlaMaxDays { get; set; }
    public DateTime SlaDueAt { get; set; }
    public DateTime SlaWarningAt { get; set; }
    public string SlaStatus { get; set; } // ON_TRACK, WARNING, BREACH
    public int? OverdueDays { get; set; }
    public double ProgressPercentage { get; set; } // 0-100
    public string CandidateName { get; set; }
    public string JobTitle { get; set; }
    public DateTime AppliedAt { get; set; }
}

/// <summary>
/// DTO cho bottleneck analysis
/// </summary>
public class StageBottleneckAnalysisDto
{
    public Guid BottleneckAnalysisId { get; set; }
    public string StageCode { get; set; }
    public string StageName { get; set; }
    public DateTime AnalysisDate { get; set; }
    public int TotalApplicationsInStage { get; set; }
    public int ApplicationsOnTrack { get; set; }
    public int ApplicationsWarning { get; set; }
    public int ApplicationsBreach { get; set; }
    public decimal AverageDaysInStage { get; set; }
    public decimal BottleneckScore { get; set; } // 0-100
    public bool IsBottleneck { get; set; }
    public decimal? PreviousDayBottleneckScore { get; set; }
    public string TrendDirection { get; set; } // IMPROVING, STABLE, WORSENING
    public int? BreachCount { get; set; }
    public double? HealthScore { get; set; } // 0-100 (green/yellow/red indicator)
}

/// <summary>
/// DTO cho SLA breach alert
/// </summary>
public class SlaBreachAlertDto
{
    public Guid AlertId { get; set; }
    public Guid ApplicationId { get; set; }
    public string AlertType { get; set; } // WARNING, BREACH
    public string StageCode { get; set; }
    public string StageName { get; set; }
    public DateTime TriggeredAt { get; set; }
    public DateTime SlaDueAt { get; set; }
    public string AlertMessage { get; set; }
    public bool IsAcknowledged { get; set; }
    public string CandidateName { get; set; }
    public string JobTitle { get; set; }
}

/// <summary>
/// DTO cho pipeline dashboard overview
/// </summary>
public class PipelineDashboardOverviewDto
{
    public int TotalApplications { get; set; }
    public int TotalOnTrack { get; set; }
    public int TotalWarning { get; set; }
    public int TotalBreach { get; set; }
    public double HealthScore { get; set; } // 0-100
    public string HealthStatus { get; set; } // EXCELLENT, GOOD, WARNING, CRITICAL
    public List<StageMetricsDto> StageMetrics { get; set; }
    public List<SlaBreachAlertDto> RecentAlerts { get; set; }
    public List<BottleneckInsightDto> BottleneckInsights { get; set; }
}

/// <summary>
/// Stage metrics for pipeline visualization
/// </summary>
public class StageMetricsDto
{
    public string StageCode { get; set; }
    public string StageName { get; set; }
    public int TotalCount { get; set; }
    public int OnTrackCount { get; set; }
    public int WarningCount { get; set; }
    public int BreachCount { get; set; }
    public decimal AverageDaysInStage { get; set; }
    public decimal BottleneckScore { get; set; }
    public bool IsBottleneck { get; set; }
    public int OrderSequence { get; set; }
}

/// <summary>
/// Bottleneck insight & recommendation
/// </summary>
public class BottleneckInsightDto
{
    public string StageCode { get; set; }
    public string StageName { get; set; }
    public decimal BottleneckScore { get; set; }
    public int ApplicationsStuck { get; set; }
    public decimal AverageDaysInStage { get; set; }
    public string Recommendation { get; set; } // "Allocate more interviewers", "Speed up offer process", etc.
    public string Severity { get; set; } // LOW, MEDIUM, HIGH, CRITICAL
}

/// <summary>
/// DTO cho bulk stage transition (move multiple candidates at once)
/// </summary>
public class BulkTransitionRequest
{
    public List<Guid> ApplicationIds { get; set; }
    public string NewStageCode { get; set; }
    public string TransitionReason { get; set; }
    public string NotesForCandidates { get; set; }
}

/// <summary>
/// DTO cho SLA compliance report
/// </summary>
public class SlaComplianceReportDto
{
    public DateTime ReportDate { get; set; }
    public int TotalApplicationsProcessed { get; set; }
    public int ApplicationsCompleted { get; set; }
    public int ApplicationsBreached { get; set; }
    public double ComplianceRate { get; set; } // 0-100
    public List<StageComplianceDto> StageCompliance { get; set; }
    public List<HrTeamComplianceDto> HrTeamCompliance { get; set; }
}

/// <summary>
/// Stage-level SLA compliance
/// </summary>
public class StageComplianceDto
{
    public string StageCode { get; set; }
    public string StageName { get; set; }
    public int TotalApplications { get; set; }
    public int OnTimeCount { get; set; }
    public int LateCount { get; set; }
    public double CompliancePercentage { get; set; }
    public decimal AverageDaysInStage { get; set; }
}

/// <summary>
/// HR team SLA compliance performance
/// </summary>
public class HrTeamComplianceDto
{
    public Guid UserId { get; set; }
    public string UserName { get; set; }
    public int ApplicationsHandled { get; set; }
    public int OnTimeCount { get; set; }
    public int LateCount { get; set; }
    public double CompliancePercentage { get; set; }
    public string PerformanceRating { get; set; } // EXCELLENT, GOOD, NEEDS_IMPROVEMENT
}

/// <summary>
/// DTO cho SLA prediction/forecast
/// </summary>
public class SlaPredictionDto
{
    public string StageCode { get; set; }
    public string StageName { get; set; }
    public int CurrentApplicationCount { get; set; }
    public int PredictedBreachCount { get; set; } // Predicted to breach in next 2 days
    public double PredictedBreachRate { get; set; }
    public string PredictedBottleneckLevel { get; set; } // NONE, LOW, MEDIUM, HIGH
    public string RecommendedAction { get; set; }
}
