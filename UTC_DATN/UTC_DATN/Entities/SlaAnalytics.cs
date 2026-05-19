namespace UTC_DATN.Entities;

public partial class SlaStageConfiguration
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
    public Guid? CreatedBy { get; set; }

    public virtual ICollection<ApplicationSlaTracking> ApplicationSlaTrackings { get; set; } = new List<ApplicationSlaTracking>();
    public virtual ICollection<StageBottleneckAnalysis> StageBottleneckAnalyses { get; set; } = new List<StageBottleneckAnalysis>();
}

public partial class ApplicationSlaTracking
{
    public Guid SlaTrackingId { get; set; }
    public Guid ApplicationId { get; set; }
    public string CurrentStageCode { get; set; }
    public DateTime EnteredStageAt { get; set; }
    public int SlaMaxDays { get; set; }
    public DateTime SlaDueAt { get; set; }
    public DateTime SlaWarningAt { get; set; }
    public string SlaStatus { get; set; } // ON_TRACK, WARNING, BREACH
    public int? OverdueDays { get; set; }
    public bool IsResolved { get; set; }
    public string? ResolutionNotes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public virtual Application Application { get; set; }
}

public partial class StageBottleneckAnalysis
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
    public decimal BottleneckScore { get; set; }
    public bool IsBottleneck { get; set; }
    public decimal? PreviousDayBottleneckScore { get; set; }
    public string? TrendDirection { get; set; } // IMPROVING, STABLE, WORSENING
    public DateTime CreatedAt { get; set; }

    public virtual SlaStageConfiguration SlaStageConfiguration { get; set; }
}

public partial class SlaBreachAlert
{
    public Guid AlertId { get; set; }
    public Guid ApplicationId { get; set; }
    public string AlertType { get; set; } // WARNING, BREACH
    public string StageCode { get; set; }
    public DateTime TriggeredAt { get; set; }
    public DateTime SlaDueAt { get; set; }
    public string AlertMessage { get; set; }
    public bool IsAcknowledged { get; set; }
    public Guid? AcknowledgedBy { get; set; }
    public DateTime? AcknowledgedAt { get; set; }
    public string? AcknowledgmentNotes { get; set; }
    public DateTime CreatedAt { get; set; }

    public virtual Application Application { get; set; }
}
