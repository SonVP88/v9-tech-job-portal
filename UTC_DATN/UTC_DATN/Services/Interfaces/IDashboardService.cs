using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UTC_DATN.DTOs.Dashboard;

namespace UTC_DATN.Services.Interfaces
{
    /// <summary>
    /// Interface for Real-time Dashboard & Predictive Analytics Service
    /// Handles metrics, predictions, forecasting, and real-time events
    /// </summary>
    public interface IDashboardService
    {
        // ============================================================
        // 1. Dashboard Overview & Metrics
        // ============================================================

        /// <summary>
        /// Get complete dashboard overview for company or specific job
        /// </summary>
        Task<DashboardOverviewDto> GetDashboardOverviewAsync(Guid? jobId = null);

        /// <summary>
        /// Get key metrics for the dashboard
        /// </summary>
        Task<MetricsOverviewDto> GetMetricsOverviewAsync(Guid? jobId = null);

        /// <summary>
        /// Get all metrics for a specific period
        /// </summary>
        Task<List<DashboardMetricDto>> GetMetricsByPeriodAsync(Guid? jobId, string periodType, DateTime fromDate, DateTime toDate);

        /// <summary>
        /// Log a new metric snapshot
        /// </summary>
        Task<DashboardMetricDto> LogMetricAsync(DashboardMetricCreateDto dto);

        // ============================================================
        // 2. Pipeline Analytics
        // ============================================================

        /// <summary>
        /// Get current pipeline state visualization
        /// </summary>
        Task<PipelineVisualizationDto> GetPipelineVisualizationAsync(Guid jobId);

        /// <summary>
        /// Get detailed pipeline snapshot
        /// </summary>
        Task<PipelineSnapshotDto> GetPipelineSnapshotAsync(Guid jobId);

        /// <summary>
        /// Get historical pipeline snapshots for trend analysis
        /// </summary>
        Task<List<PipelineSnapshotDto>> GetPipelineHistoryAsync(Guid jobId, int days = 30);

        /// <summary>
        /// Get pipeline health report with issues and recommendations
        /// </summary>
        Task<PipelineHealthReportDto> GetPipelineHealthReportAsync(Guid jobId);

        /// <summary>
        /// Get stage breakdown (counts and percentages)
        /// </summary>
        Task<List<PipelineStageDto>> GetStageBreakdownAsync(Guid jobId);

        // ============================================================
        // 3. Candidate Progress Tracking
        // ============================================================

        /// <summary>
        /// Get candidate's complete journey through pipeline
        /// </summary>
        Task<CandidateJourneyDto> GetCandidateJourneyAsync(Guid candidateId);

        /// <summary>
        /// Get progress history for all candidates in a job
        /// </summary>
        Task<List<CandidateProgressHistoryDto>> GetJobProgressHistoryAsync(Guid jobId, DateTime fromDate, DateTime toDate);

        /// <summary>
        /// Record candidate stage transition
        /// </summary>
        Task<CandidateProgressHistoryDto> RecordProgressAsync(CandidateProgressCreateDto dto);

        /// <summary>
        /// Get average time in each stage
        /// </summary>
        Task<Dictionary<string, decimal>> GetAverageTimePerStageAsync(Guid jobId);

        // ============================================================
        // 4. Predictions
        // ============================================================

        /// <summary>
        /// Get prediction for an application (offer acceptance, dropout risk, etc.)
        /// </summary>
        Task<PredictionResultDto> GetPredictionAsync(PredictionRequestDto request);

        /// <summary>
        /// Get offer acceptance prediction for a candidate
        /// </summary>
        Task<OfferAcceptancePredictionDto> GetOfferAcceptancePredictionAsync(Guid applicationId);

        /// <summary>
        /// Get time-to-hire prediction for a candidate
        /// </summary>
        Task<TimeToHirePredictionDto> GetTimeToHirePredictionAsync(Guid applicationId);

        /// <summary>
        /// Get dropout risk prediction for a candidate
        /// </summary>
        Task<DropoutRiskPredictionDto> GetDropoutRiskPredictionAsync(Guid applicationId);

        /// <summary>
        /// Get all critical predictions (high risk candidates)
        /// </summary>
        Task<List<PredictionResultDto>> GetCriticalPredictionsAsync(Guid jobId);

        /// <summary>
        /// Save prediction result to database
        /// </summary>
        Task<PredictionResultDto> SavePredictionAsync(PredictionResultDto prediction);

        /// <summary>
        /// Update prediction with actual outcome for model validation
        /// </summary>
        Task UpdatePredictionWithOutcomeAsync(Guid resultId, string actualOutcome);

        /// <summary>
        /// Get model performance metrics
        /// </summary>
        Task<PredictionModelDto> GetModelMetricsAsync(Guid modelId);

        /// <summary>
        /// List all available prediction models
        /// </summary>
        Task<List<PredictionModelDto>> GetAllModelsAsync();

        // ============================================================
        // 5. Forecasting
        // ============================================================

        /// <summary>
        /// Generate forecasts for job (time-to-hire, conversion rate, etc.)
        /// </summary>
        Task<List<ForecastingDataDto>> GenerateForecastsAsync(GenerateForecastsRequestDto request);

        /// <summary>
        /// Get time-to-hire forecast for a job
        /// </summary>
        Task<TimeToHireForecastDto> GetTimeToHireForcastAsync(Guid jobId, int daysAhead = 30);

        /// <summary>
        /// Get conversion rate forecast
        /// </summary>
        Task<ForecastingTrendDto> GetConversionRateForecastAsync(Guid jobId, int daysAhead = 30);

        /// <summary>
        /// Get all forecasts for a job
        /// </summary>
        Task<List<ForecastingDataDto>> GetJobForecastsAsync(Guid jobId);

        /// <summary>
        /// Validate forecast accuracy with actual data
        /// </summary>
        Task ValidateForecastAsync(Guid forecastId);

        // ============================================================
        // 6. Real-time Events
        // ============================================================

        /// <summary>
        /// Publish real-time event for WebSocket broadcast
        /// </summary>
        Task PublishEventAsync(RealTimeEventPublishDto eventDto);

        /// <summary>
        /// Get unprocessed events for WebSocket broadcast
        /// </summary>
        Task<List<RealTimeEventDto>> GetPendingEventsAsync(int limit = 100);

        /// <summary>
        /// Mark events as processed
        /// </summary>
        Task MarkEventsAsProcessedAsync(List<Guid> eventIds);

        /// <summary>
        /// Get recent events for a job
        /// </summary>
        Task<List<RealTimeEventDto>> GetRecentEventsAsync(Guid jobId, int minutes = 60);

        // ============================================================
        // 7. Analytics & Reporting
        // ============================================================

        /// <summary>
        /// Get comprehensive job analytics
        /// </summary>
        Task<JobAnalyticsDto> GetJobAnalyticsAsync(Guid jobId);

        /// <summary>
        /// Get company-wide analytics
        /// </summary>
        Task<CompanyAnalyticsDto> GetCompanyAnalyticsAsync();

        /// <summary>
        /// Get performance metrics and trends
        /// </summary>
        Task<PerformanceReportDto> GetPerformanceReportAsync(Guid? jobId = null, int days = 30);

        /// <summary>
        /// Get leaderboard of top candidates
        /// </summary>
        Task<LeaderboardDto> GetLeaderboardAsync(Guid jobId, int topCount = 50);

        /// <summary>
        /// Export analytics data
        /// </summary>
        Task<byte[]> ExportAnalyticsAsync(Guid jobId, string format = "xlsx");

        // ============================================================
        // 8. Cache Management
        // ============================================================

        /// <summary>
        /// Refresh dashboard cache
        /// </summary>
        Task RefreshDashboardCacheAsync(DashboardRefreshRequestDto request);

        /// <summary>
        /// Get cache statistics
        /// </summary>
        Task<CacheStatsDto> GetCacheStatsAsync();

        /// <summary>
        /// Clear all cache
        /// </summary>
        Task ClearCacheAsync();

        /// <summary>
        /// Clear specific cache entry
        /// </summary>
        Task ClearCacheKeyAsync(string cacheKey);

        // ============================================================
        // 9. Bulk Operations
        // ============================================================

        /// <summary>
        /// Recalculate all rankings for job
        /// </summary>
        Task RecalculateRankingsAsync(RecalculateRankingsRequestDto request);

        /// <summary>
        /// Generate predictions for all candidates in job
        /// </summary>
        Task BulkGeneratePredictionsAsync(Guid jobId);

        /// <summary>
        /// Generate forecasts for all open jobs
        /// </summary>
        Task BulkGenerateForecastsAsync();

        /// <summary>
        /// Update all metrics snapshots
        /// </summary>
        Task UpdateAllMetricsAsync();

        // ============================================================
        // 10. Background Jobs (Scheduled Operations)
        // ============================================================

        /// <summary>
        /// Hourly background task - update metrics
        /// </summary>
        Task RunHourlyMaintenanceAsync();

        /// <summary>
        /// Daily background task - generate forecasts, clean old events
        /// </summary>
        Task RunDailyMaintenanceAsync();

        /// <summary>
        /// Weekly background task - generate reports, validate predictions
        /// </summary>
        Task RunWeeklyMaintenanceAsync();

        // ============================================================
        // 11. Advanced Analytics
        // ============================================================

        /// <summary>
        /// Get hiring velocity (candidates/week)
        /// </summary>
        Task<decimal> GetHiringVelocityAsync(Guid? jobId = null);

        /// <summary>
        /// Get quality of hire score
        /// </summary>
        Task<decimal> GetQualityOfHireAsync(Guid? jobId = null);

        /// <summary>
        /// Get cost per hire
        /// </summary>
        Task<decimal> GetCostPerHireAsync(Guid? jobId = null);

        /// <summary>
        /// Get time-to-productivity estimate
        /// </summary>
        Task<decimal> GetTimeToProductivityAsync(Guid? jobId = null);

        /// <summary>
        /// Get trends for a metric over time
        /// </summary>
        Task<List<TrendDataPointDto>> GetMetricTrendAsync(string metricCode, int days = 30);

        // ============================================================
        // 12. Admin Operations
        // ============================================================

        /// <summary>
        /// Reset all dashboard data for a job
        /// </summary>
        Task ResetJobDashboardDataAsync(Guid jobId);

        /// <summary>
        /// Reprocess prediction results
        /// </summary>
        Task ReprocessPredictionsAsync(Guid jobId);

        /// <summary>
        /// Get dashboard operation logs
        /// </summary>
        Task<List<DashboardOperationLogDto>> GetOperationLogsAsync(int lastCount = 100);
    }

    /// <summary>
    /// DTO for trend data points
    /// </summary>
    public class TrendDataPointDto
    {
        public DateTime Date { get; set; }
        public decimal Value { get; set; }
        public decimal? PreviousValue { get; set; }
        public decimal? PercentageChange { get; set; }
    }

    /// <summary>
    /// DTO for dashboard operation logs
    /// </summary>
    public class DashboardOperationLogDto
    {
        public Guid LogId { get; set; }
        public DateTime Timestamp { get; set; }
        public string Operation { get; set; }
        public string Status { get; set; }           // SUCCESS, FAILED, IN_PROGRESS
        public string ErrorMessage { get; set; }
        public long DurationMs { get; set; }
    }
}
