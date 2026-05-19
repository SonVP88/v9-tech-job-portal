using UTC_DATN.DTOs.Sla;

namespace UTC_DATN.Services.Interfaces;

/// <summary>
/// Service for SLA tracking, bottleneck analysis, and pipeline insights
/// </summary>
public interface ISlaAnalyticsService
{
    // ==================== SLA Configuration ====================
    /// <summary>
    /// Get all SLA stage configurations
    /// </summary>
    Task<List<SlaStageConfigDto>> GetAllStageConfigurationsAsync();

    /// <summary>
    /// Get SLA config for specific stage
    /// </summary>
    Task<SlaStageConfigDto?> GetStageConfigurationAsync(string stageCode);

    /// <summary>
    /// Update SLA configuration
    /// </summary>
    Task<bool> UpdateStageConfigurationAsync(string stageCode, UpsertSlaStageConfigDto dto);

    // ==================== Application SLA Tracking ====================
    /// <summary>
    /// Get SLA tracking for a specific application
    /// </summary>
    Task<ApplicationSlaTrackingDto?> GetApplicationSlaTrackingAsync(Guid applicationId);

    /// <summary>
    /// Update application to new stage and recalculate SLA
    /// </summary>
    Task<bool> UpdateApplicationStageAsync(Guid applicationId, string newStageCode, string? notes = null);

    /// <summary>
    /// Move multiple applications to new stage (bulk operation)
    /// </summary>
    Task<bool> BulkTransitionApplicationsAsync(BulkTransitionRequest request, Guid userId);

    // ==================== Bottleneck Analysis ====================
    /// <summary>
    /// Get bottleneck analysis for all stages (daily snapshot)
    /// </summary>
    Task<List<StageBottleneckAnalysisDto>> GetAllBottleneckAnalysisAsync();

    /// <summary>
    /// Get bottleneck analysis for specific stage
    /// </summary>
    Task<StageBottleneckAnalysisDto?> GetStageBottleneckAnalysisAsync(string stageCode);

    /// <summary>
    /// Manually trigger bottleneck analysis calculation
    /// </summary>
    Task<bool> CalculateBottleneckAnalysisAsync();

    /// <summary>
    /// Get top bottleneck stages with severity
    /// </summary>
    Task<List<BottleneckInsightDto>> GetBottleneckInsightsAsync(int topCount = 5);

    // ==================== SLA Breach Alerts ====================
    /// <summary>
    /// Get all active SLA breach alerts
    /// </summary>
    Task<List<SlaBreachAlertDto>> GetActiveAlertsAsync(int pageSize = 20);

    /// <summary>
    /// Get alerts for specific application
    /// </summary>
    Task<List<SlaBreachAlertDto>> GetApplicationAlertsAsync(Guid applicationId);

    /// <summary>
    /// Acknowledge a breach alert
    /// </summary>
    Task<bool> AcknowledgeAlertAsync(Guid alertId, string? notes, Guid userId);

    /// <summary>
    /// Get count of unacknowledged alerts by severity
    /// </summary>
    Task<(int warningCount, int breachCount)> GetUnacknowledgedAlertCountAsync();

    // ==================== Pipeline Dashboard ====================
    /// <summary>
    /// Get comprehensive pipeline dashboard overview
    /// </summary>
    Task<PipelineDashboardOverviewDto> GetPipelineDashboardAsync();

    /// <summary>
    /// Get detailed metrics for each stage
    /// </summary>
    Task<List<StageMetricsDto>> GetPipelineStageMetricsAsync();

    /// <summary>
    /// Get applications at risk of SLA breach in next N days
    /// </summary>
    Task<List<ApplicationSlaTrackingDto>> GetApplicationsAtRiskAsync(int daysAhead = 2);

    // ==================== Reporting ====================
    /// <summary>
    /// Generate SLA compliance report for given date range
    /// </summary>
    Task<SlaComplianceReportDto> GenerateComplianceReportAsync(DateTime fromDate, DateTime toDate);

    /// <summary>
    /// Get SLA predictions/forecasts for next N days
    /// </summary>
    Task<List<SlaPredictionDto>> GetSlapredictionsAsync(int daysAhead = 7);

    /// <summary>
    /// Get HR team performance ranked by SLA compliance
    /// </summary>
    Task<List<HrTeamComplianceDto>> GetTeamComplianceRankingAsync();

    // ==================== Utilities ====================
    /// <summary>
    /// Calculate health score (0-100) for entire pipeline
    /// </summary>
    Task<double> CalculatePipelineHealthScoreAsync();

    /// <summary>
    /// Force recalculate SLA for all active applications
    /// </summary>
    Task<bool> RecalculateAllSlasAsync();
}
