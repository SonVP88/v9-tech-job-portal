using System;
using System.Collections.Generic;

namespace UTC_DATN.DTOs.Dashboard
{
    // ============================================================
    // 1. Dashboard Metrics DTOs
    // ============================================================
    
    /// <summary>
    /// Dashboard Metric - KPI snapshot (hourly, daily, weekly)
    /// </summary>
    public class DashboardMetricDto
    {
        public Guid MetricId { get; set; }
        public string MetricCode { get; set; }        // TOTAL_APPLICATIONS, PIPELINE_HEALTH
        public string MetricName { get; set; }
        public decimal MetricValue { get; set; }
        public string MetricUnit { get; set; }         // %, days, count, $
        public string TrendDirection { get; set; }     // UP, DOWN, STABLE
        public decimal? ComparisonPeriodValue { get; set; }
        public decimal? PercentageChange { get; set; }
        public DateTime RecordedAt { get; set; }
        public string PeriodType { get; set; }         // HOURLY, DAILY, WEEKLY
        public Guid? JobId { get; set; }
    }

    public class DashboardMetricCreateDto
    {
        public string MetricCode { get; set; }
        public string MetricName { get; set; }
        public decimal MetricValue { get; set; }
        public string MetricUnit { get; set; }
        public Guid? JobId { get; set; }
        public string PeriodType { get; set; } = "DAILY";
    }

    /// <summary>
    /// Multiple metrics for dashboard overview
    /// </summary>
    public class MetricsOverviewDto
    {
        public List<DashboardMetricDto> TodayMetrics { get; set; }
        public List<DashboardMetricDto> WeekMetrics { get; set; }
        public Dictionary<string, decimal?> KeyIndicators { get; set; }
        public DateTime LastUpdated { get; set; }
    }

    // ============================================================
    // 2. Pipeline Snapshot DTOs
    // ============================================================

    public class PipelineSnapshotDto
    {
        public Guid SnapshotId { get; set; }
        public Guid JobId { get; set; }
        public DateTime SnapshotTimestamp { get; set; }
        public int TotalApplications { get; set; }
        public int ScreeningCount { get; set; }
        public int InterviewCount { get; set; }
        public int OfferCount { get; set; }
        public int RejectedCount { get; set; }
        public int HiredCount { get; set; }
        public int DropoutCount { get; set; }
        public int? DaysToFill { get; set; }
        public Dictionary<string, decimal> AverageTimePerStage { get; set; }
        public decimal? ConversionRate { get; set; }
        public int? HealthScore { get; set; }
    }

    public class PipelineVisualizationDto
    {
        public Guid JobId { get; set; }
        public List<PipelineStageDto> Stages { get; set; }
        public int TotalCandidates { get; set; }
        public decimal ConversionRate { get; set; }
        public decimal DropoutRate { get; set; }
    }

    public class PipelineStageDto
    {
        public string StageName { get; set; }
        public int Count { get; set; }
        public decimal PercentageOfTotal { get; set; }
        public int StageOrder { get; set; }
    }

    public class PipelineHealthReportDto
    {
        public Guid JobId { get; set; }
        public int HealthScore { get; set; }
        public string HealthStatus { get; set; }         // EXCELLENT, GOOD, WARNING, CRITICAL
        public Dictionary<string, string> Issues { get; set; }
        public List<string> Recommendations { get; set; }
        public DateTime AssessedAt { get; set; }
    }

    // ============================================================
    // 3. Candidate Progress History DTOs
    // ============================================================

    public class CandidateProgressHistoryDto
    {
        public Guid ProgressId { get; set; }
        public Guid ApplicationId { get; set; }
        public Guid CandidateId { get; set; }
        public Guid JobId { get; set; }
        public string PreviousStage { get; set; }
        public string NewStage { get; set; }
        public string StageChangeReason { get; set; }
        public int? TimeInPreviousStage { get; set; }
        public DateTime MovedAt { get; set; }
        public Guid MovedByUserId { get; set; }
        public bool IsAutomatic { get; set; }
        public int? ProcessingDuration { get; set; }
    }

    public class CandidateProgressCreateDto
    {
        public Guid ApplicationId { get; set; }
        public string NewStage { get; set; }
        public string StageChangeReason { get; set; }
    }

    public class CandidateJourneyDto
    {
        public Guid CandidateId { get; set; }
        public string CandidateName { get; set; }
        public string JobTitle { get; set; }
        public List<JourneyEventDto> Journey { get; set; }
        public TimeSpan TotalDuration { get; set; }
    }

    public class JourneyEventDto
    {
        public string Stage { get; set; }
        public DateTime Timestamp { get; set; }
        public int DaysInStage { get; set; }
        public string Action { get; set; }
    }

    // ============================================================
    // 4. Prediction DTOs
    // ============================================================

    public class PredictionModelDto
    {
        public Guid ModelId { get; set; }
        public string ModelName { get; set; }
        public string ModelType { get; set; }             // CLASSIFICATION, REGRESSION
        public string PredictionTarget { get; set; }      // OFFER_ACCEPTANCE, TIME_TO_HIRE
        public string ModelVersion { get; set; }
        public decimal? Accuracy { get; set; }
        public decimal? Precision { get; set; }
        public decimal? Recall { get; set; }
        public decimal? F1Score { get; set; }
        public bool IsActive { get; set; }
        public List<string> InputFeatures { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class PredictionResultDto
    {
        public Guid ResultId { get; set; }
        public Guid ModelId { get; set; }
        public Guid ApplicationId { get; set; }
        public string PredictionTarget { get; set; }
        public decimal PredictionValue { get; set; }       // 0.85 (85%), 21 (days)
        public decimal ConfidenceScore { get; set; }       // 0-100%
        public string PredictionCategory { get; set; }     // HIGH_RISK, MEDIUM, LOW_RISK
        public Dictionary<string, decimal> FeatureContribution { get; set; }
        public List<string> RiskFactors { get; set; }
        public List<string> Recommendations { get; set; }
        public DateTime PredictedAt { get; set; }
    }

    public class PredictionRequestDto
    {
        public Guid ApplicationId { get; set; }
        public string PredictionTarget { get; set; }       // OFFER_ACCEPTANCE, TIME_TO_HIRE, DROPOUT_RISK
    }

    public class OfferAcceptancePredictionDto : PredictionResultDto
    {
        public decimal AcceptanceProbability { get; set; }  // 0-1
        public string RiskLevel { get; set; }               // LOW, MEDIUM, HIGH
        public List<string> NegotiationHints { get; set; }
    }

    public class TimeToHirePredictionDto : PredictionResultDto
    {
        public int PredictedDays { get; set; }
        public int LowerBound { get; set; }
        public int UpperBound { get; set; }
        public List<string> AccelerationTips { get; set; }
    }

    public class DropoutRiskPredictionDto : PredictionResultDto
    {
        public decimal DropoutProbability { get; set; }     // 0-1
        public List<string> RiskIndicators { get; set; }
        public List<string> RetentionStrategies { get; set; }
    }

    // ============================================================
    // 5. Forecasting DTOs
    // ============================================================

    public class ForecastingDataDto
    {
        public Guid ForecastId { get; set; }
        public Guid JobId { get; set; }
        public string ForecastType { get; set; }           // TIME_TO_HIRE, CONVERSION_RATE
        public DateTime ForecastDate { get; set; }
        public decimal ForecastedValue { get; set; }
        public decimal? LowerBound { get; set; }
        public decimal? UpperBound { get; set; }
        public decimal? ActualValue { get; set; }
        public int? Confidence { get; set; }
        public DateTime CalculatedAt { get; set; }
    }

    public class ForecastingTrendDto
    {
        public Guid JobId { get; set; }
        public string ForecastType { get; set; }
        public List<ForecastPointDto> ForecastPoints { get; set; }
        public string Trend { get; set; }                   // IMPROVING, DECLINING, STABLE
        public string MethodUsed { get; set; }              // LINEAR_REGRESSION, ARIMA, ML
    }

    public class ForecastPointDto
    {
        public DateTime Date { get; set; }
        public decimal ForecastedValue { get; set; }
        public decimal? ActualValue { get; set; }
        public decimal? LowerBound { get; set; }
        public decimal? UpperBound { get; set; }
    }

    public class TimeToHireForecastDto
    {
        public Guid JobId { get; set; }
        public int AverageDaysToHire { get; set; }
        public int ForecastedDaysToFill { get; set; }
        public DateTime ProbableCompletionDate { get; set; }
        public int Confidence { get; set; }
        public List<ForecastPointDto> WeeklyForecast { get; set; }
    }

    // ============================================================
    // 6. Real-time Event DTOs
    // ============================================================

    public class RealTimeEventDto
    {
        public Guid EventId { get; set; }
        public string EventType { get; set; }               // APPLICATION_RECEIVED, STAGE_CHANGED
        public string EventSource { get; set; }             // SYSTEM, USER_ACTION, AUTO_WORKFLOW
        public Guid? ApplicationId { get; set; }
        public Guid? JobId { get; set; }
        public Dictionary<string, object> EventData { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class RealTimeEventPublishDto
    {
        public string EventType { get; set; }
        public Guid? ApplicationId { get; set; }
        public Guid? JobId { get; set; }
        public Dictionary<string, object> Payload { get; set; }
    }

    // ============================================================
    // 7. Analytics Dashboard DTOs
    // ============================================================

    public class DashboardOverviewDto
    {
        public Guid? JobId { get; set; }
        public string JobTitle { get; set; }

        // Key Metrics
        public int TotalApplications { get; set; }
        public int ApplicationsToday { get; set; }
        public decimal ConversionRate { get; set; }
        public int DaysToFill { get; set; }

        // Pipeline Status
        public PipelineVisualizationDto PipelineData { get; set; }

        // Health & Predictions
        public PipelineHealthReportDto HealthReport { get; set; }
        public List<PredictionResultDto> CriticalPredictions { get; set; }

        // Recent Activity
        public List<RealTimeEventDto> RecentEvents { get; set; }

        // Forecasts
        public TimeToHireForecastDto TimeToHireForecast { get; set; }

        public DateTime LastUpdated { get; set; }
    }

    public class JobAnalyticsDto
    {
        public Guid JobId { get; set; }
        public string JobTitle { get; set; }
        public int TotalCandidates { get; set; }
        public int HiredCount { get; set; }
        public decimal SuccessRate { get; set; }
        public int AverageDaysToHire { get; set; }
        public decimal AverageCandidateScore { get; set; }
        public List<PipelineStageDto> StageDistribution { get; set; }
        public Dictionary<string, int> MonthlyTrend { get; set; }
    }

    public class CompanyAnalyticsDto
    {
        public int TotalOpenPositions { get; set; }
        public int TotalApplications { get; set; }
        public decimal AverageConversionRate { get; set; }
        public int AverageDaysToFill { get; set; }
        public List<JobAnalyticsDto> JobAnalytics { get; set; }
        public Dictionary<string, decimal> MetricsTrends { get; set; }
        public DateTime GeneratedAt { get; set; }
    }

    // ============================================================
    // 8. Analytics Cache DTOs
    // ============================================================

    public class AnalyticsCacheDto
    {
        public Guid CacheId { get; set; }
        public string CacheKey { get; set; }
        public string DataType { get; set; }
        public DateTime ExpiresAt { get; set; }
        public int HitCount { get; set; }
        public int RefreshCount { get; set; }
    }

    public class CacheStatsDto
    {
        public int TotalCacheEntries { get; set; }
        public int ExpiredEntries { get; set; }
        public decimal CacheHitRate { get; set; }
        public TimeSpan AverageCacheAge { get; set; }
    }

    // ============================================================
    // 9. Leaderboard & Ranking DTOs
    // ============================================================

    public class CandidateLeaderboardDto
    {
        public Guid CandidateId { get; set; }
        public string CandidateName { get; set; }
        public string Email { get; set; }
        public int Rank { get; set; }
        public decimal OverallScore { get; set; }
        public decimal AssessmentScore { get; set; }
        public decimal InterviewScore { get; set; }
        public int DaysInPipeline { get; set; }
    }

    public class LeaderboardDto
    {
        public Guid JobId { get; set; }
        public string JobTitle { get; set; }
        public List<CandidateLeaderboardDto> TopCandidates { get; set; }
        public int TotalCandidates { get; set; }
    }

    // ============================================================
    // 10. Performance Metrics DTOs
    // ============================================================

    public class PerformanceMetricsDto
    {
        public decimal HiringVelocity { get; set; }         // Candidates/week
        public decimal QualityOfHire { get; set; }          // 0-100 score
        public decimal DropoutRate { get; set; }            // %
        public decimal TimeToProductivity { get; set; }     // Days
        public decimal CostPerHire { get; set; }            // $
        public decimal RoundDuration { get; set; }          // Average days per round
    }

    public class PerformanceTrendDto
    {
        public DateTime Date { get; set; }
        public PerformanceMetricsDto Metrics { get; set; }
    }

    public class PerformanceReportDto
    {
        public DateTime ReportDate { get; set; }
        public PerformanceMetricsDto CurrentMetrics { get; set; }
        public PerformanceMetricsDto PreviousPeriodMetrics { get; set; }
        public List<PerformanceTrendDto> TrendHistory { get; set; }
        public List<string> Insights { get; set; }
        public List<string> Recommendations { get; set; }
    }

    // ============================================================
    // 11. Bulk Operations DTOs
    // ============================================================

    public class DashboardRefreshRequestDto
    {
        public Guid? JobId { get; set; }
        public List<string> MetricTypes { get; set; }       // What metrics to refresh
        public bool ForceRefresh { get; set; } = false;
    }

    public class GenerateForecastsRequestDto
    {
        public Guid JobId { get; set; }
        public List<string> ForecastTypes { get; set; }     // TIME_TO_HIRE, CONVERSION_RATE
        public int DaysAhead { get; set; } = 30;
    }

    public class RecalculateRankingsRequestDto
    {
        public Guid? JobId { get; set; }
        public string RankingCriteria { get; set; } = "OVERALL";  // ASSESSMENT, INTERVIEW, OVERALL
    }
}
