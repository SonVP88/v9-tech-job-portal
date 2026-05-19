using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using UTC_DATN.DTOs.Dashboard;
using UTC_DATN.Services.Interfaces;

namespace UTC_DATN.Controllers
{
    /// <summary>
    /// Real-time Dashboard & Predictive Analytics API Controller (MODULE_10)
    /// Handles dashboard metrics, predictions, forecasting, and real-time events
    /// </summary>
    [ApiController]
    [Route("api/v2/dashboard")]
    [Authorize]
    public class DashboardAnalyticsController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;
        private readonly ILogger<DashboardAnalyticsController> _logger;

        public DashboardAnalyticsController(IDashboardService dashboardService, ILogger<DashboardAnalyticsController> logger)
        {
            _dashboardService = dashboardService;
            _logger = logger;
        }

        // ============================================================
        // 1. Dashboard Overview & Metrics
        // ============================================================

        /// <summary>
        /// Get complete dashboard overview for company or specific job
        /// GET /api/v2/dashboard/overview
        /// </summary>
        [HttpGet("overview")]
        public async Task<ActionResult<DashboardOverviewDto>> GetDashboardOverview(Guid? jobId = null)
        {
            try
            {
                var overview = await _dashboardService.GetDashboardOverviewAsync(jobId);
                return Ok(new { success = true, data = overview });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting dashboard overview: {ex.Message}");
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        /// <summary>
        /// Get key metrics overview
        /// GET /api/v2/dashboard/metrics
        /// </summary>
        [HttpGet("metrics")]
        public async Task<ActionResult<MetricsOverviewDto>> GetMetricsOverview(Guid? jobId = null)
        {
            try
            {
                var metrics = await _dashboardService.GetMetricsOverviewAsync(jobId);
                return Ok(new { success = true, data = metrics });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting metrics overview: {ex.Message}");
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        /// <summary>
        /// Get metrics for specific period
        /// GET /api/v2/dashboard/metrics/period?periodType=DAILY&fromDate=2026-05-01&toDate=2026-05-31
        /// </summary>
        [HttpGet("metrics/period")]
        public async Task<ActionResult<List<DashboardMetricDto>>> GetMetricsByPeriod(
            Guid? jobId,
            string periodType = "DAILY",
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null)
        {
            try
            {
                var from = fromDate ?? DateTime.UtcNow.AddDays(-30);
                var to = toDate ?? DateTime.UtcNow;
                var metrics = await _dashboardService.GetMetricsByPeriodAsync(jobId, periodType, from, to);
                return Ok(new { success = true, data = metrics });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting metrics by period: {ex.Message}");
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        /// <summary>
        /// Log new metric
        /// POST /api/v2/dashboard/metrics
        /// </summary>
        [HttpPost("metrics")]
        [Authorize(Roles = "ADMIN,HR")]
        public async Task<ActionResult<DashboardMetricDto>> LogMetric(DashboardMetricCreateDto dto)
        {
            try
            {
                var metric = await _dashboardService.LogMetricAsync(dto);
                return CreatedAtAction(nameof(GetDashboardOverview), new { }, new { success = true, data = metric });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error logging metric: {ex.Message}");
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        // ============================================================
        // 2. Pipeline Analytics
        // ============================================================

        /// <summary>
        /// Get pipeline visualization
        /// GET /api/v2/dashboard/pipeline/{jobId}/visualization
        /// </summary>
        [HttpGet("pipeline/{jobId}/visualization")]
        public async Task<ActionResult<PipelineVisualizationDto>> GetPipelineVisualization(Guid jobId)
        {
            try
            {
                var pipeline = await _dashboardService.GetPipelineVisualizationAsync(jobId);
                return Ok(new { success = true, data = pipeline });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { success = false, error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting pipeline visualization: {ex.Message}");
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        /// <summary>
        /// Get pipeline health report
        /// GET /api/v2/dashboard/pipeline/{jobId}/health
        /// </summary>
        [HttpGet("pipeline/{jobId}/health")]
        public async Task<ActionResult<PipelineHealthReportDto>> GetPipelineHealth(Guid jobId)
        {
            try
            {
                var health = await _dashboardService.GetPipelineHealthReportAsync(jobId);
                return Ok(new { success = true, data = health });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting pipeline health: {ex.Message}");
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        /// <summary>
        /// Get stage breakdown
        /// GET /api/v2/dashboard/pipeline/{jobId}/stages
        /// </summary>
        [HttpGet("pipeline/{jobId}/stages")]
        public async Task<ActionResult<List<PipelineStageDto>>> GetStageBreakdown(Guid jobId)
        {
            try
            {
                var stages = await _dashboardService.GetStageBreakdownAsync(jobId);
                return Ok(new { success = true, data = stages });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting stage breakdown: {ex.Message}");
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        // ============================================================
        // 3. Predictions
        // ============================================================

        /// <summary>
        /// Get offer acceptance prediction
        /// GET /api/v2/dashboard/predict/{applicationId}/offer-acceptance
        /// </summary>
        [HttpGet("predict/{applicationId}/offer-acceptance")]
        public async Task<ActionResult<OfferAcceptancePredictionDto>> GetOfferAcceptancePrediction(Guid applicationId)
        {
            try
            {
                var prediction = await _dashboardService.GetOfferAcceptancePredictionAsync(applicationId);
                return Ok(new { success = true, data = prediction });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting offer acceptance prediction: {ex.Message}");
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        /// <summary>
        /// Get time-to-hire prediction
        /// GET /api/v2/dashboard/predict/{applicationId}/time-to-hire
        /// </summary>
        [HttpGet("predict/{applicationId}/time-to-hire")]
        public async Task<ActionResult<TimeToHirePredictionDto>> GetTimeToHirePrediction(Guid applicationId)
        {
            try
            {
                var prediction = await _dashboardService.GetTimeToHirePredictionAsync(applicationId);
                return Ok(new { success = true, data = prediction });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting time-to-hire prediction: {ex.Message}");
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        /// <summary>
        /// Get dropout risk prediction
        /// GET /api/v2/dashboard/predict/{applicationId}/dropout-risk
        /// </summary>
        [HttpGet("predict/{applicationId}/dropout-risk")]
        public async Task<ActionResult<DropoutRiskPredictionDto>> GetDropoutRiskPrediction(Guid applicationId)
        {
            try
            {
                var prediction = await _dashboardService.GetDropoutRiskPredictionAsync(applicationId);
                return Ok(new { success = true, data = prediction });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting dropout risk prediction: {ex.Message}");
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        /// <summary>
        /// Get critical predictions for job (high-risk candidates)
        /// GET /api/v2/dashboard/jobs/{jobId}/critical-predictions
        /// </summary>
        [HttpGet("jobs/{jobId}/critical-predictions")]
        public async Task<ActionResult<List<PredictionResultDto>>> GetCriticalPredictions(Guid jobId)
        {
            try
            {
                var predictions = await _dashboardService.GetCriticalPredictionsAsync(jobId);
                return Ok(new { success = true, data = predictions });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting critical predictions: {ex.Message}");
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        // ============================================================
        // 4. Forecasting
        // ============================================================

        /// <summary>
        /// Get time-to-hire forecast
        /// GET /api/v2/dashboard/jobs/{jobId}/forecast/time-to-hire?daysAhead=30
        /// </summary>
        [HttpGet("jobs/{jobId}/forecast/time-to-hire")]
        public async Task<ActionResult<TimeToHireForecastDto>> GetTimeToHireForecast(Guid jobId, int daysAhead = 30)
        {
            try
            {
                var forecast = await _dashboardService.GetTimeToHireForcastAsync(jobId, daysAhead);
                return Ok(new { success = true, data = forecast });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting time-to-hire forecast: {ex.Message}");
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        /// <summary>
        /// Get conversion rate forecast
        /// GET /api/v2/dashboard/jobs/{jobId}/forecast/conversion-rate
        /// </summary>
        [HttpGet("jobs/{jobId}/forecast/conversion-rate")]
        public async Task<ActionResult<ForecastingTrendDto>> GetConversionRateForecast(Guid jobId, int daysAhead = 30)
        {
            try
            {
                var forecast = await _dashboardService.GetConversionRateForecastAsync(jobId, daysAhead);
                return Ok(new { success = true, data = forecast });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting conversion rate forecast: {ex.Message}");
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        // ============================================================
        // 5. Analytics & Reporting
        // ============================================================

        /// <summary>
        /// Get job analytics
        /// GET /api/v2/dashboard/jobs/{jobId}/analytics
        /// </summary>
        [HttpGet("jobs/{jobId}/analytics")]
        public async Task<ActionResult<JobAnalyticsDto>> GetJobAnalytics(Guid jobId)
        {
            try
            {
                var analytics = await _dashboardService.GetJobAnalyticsAsync(jobId);
                return Ok(new { success = true, data = analytics });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting job analytics: {ex.Message}");
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        /// <summary>
        /// Get company analytics
        /// GET /api/v2/dashboard/analytics/company
        /// </summary>
        [HttpGet("analytics/company")]
        [Authorize(Roles = "ADMIN")]
        public async Task<ActionResult<CompanyAnalyticsDto>> GetCompanyAnalytics()
        {
            try
            {
                var analytics = await _dashboardService.GetCompanyAnalyticsAsync();
                return Ok(new { success = true, data = analytics });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting company analytics: {ex.Message}");
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        /// <summary>
        /// Get performance report
        /// GET /api/v2/dashboard/reports/performance?jobId=&days=30
        /// </summary>
        [HttpGet("reports/performance")]
        public async Task<ActionResult<PerformanceReportDto>> GetPerformanceReport(Guid? jobId = null, int days = 30)
        {
            try
            {
                var report = await _dashboardService.GetPerformanceReportAsync(jobId, days);
                return Ok(new { success = true, data = report });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting performance report: {ex.Message}");
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        /// <summary>
        /// Get leaderboard
        /// GET /api/v2/dashboard/jobs/{jobId}/leaderboard?topCount=50
        /// </summary>
        [HttpGet("jobs/{jobId}/leaderboard")]
        public async Task<ActionResult<LeaderboardDto>> GetLeaderboard(Guid jobId, int topCount = 50)
        {
            try
            {
                var leaderboard = await _dashboardService.GetLeaderboardAsync(jobId, topCount);
                return Ok(new { success = true, data = leaderboard });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting leaderboard: {ex.Message}");
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        // ============================================================
        // 6. Real-time Events
        // ============================================================

        /// <summary>
        /// Get pending events for WebSocket broadcast
        /// GET /api/v2/dashboard/events/pending?limit=100
        /// </summary>
        [HttpGet("events/pending")]
        [Authorize(Roles = "ADMIN,SYSTEM")]
        public async Task<ActionResult<List<RealTimeEventDto>>> GetPendingEvents(int limit = 100)
        {
            try
            {
                var events = await _dashboardService.GetPendingEventsAsync(limit);
                return Ok(new { success = true, data = events });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting pending events: {ex.Message}");
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        /// <summary>
        /// Get recent events for a job
        /// GET /api/v2/dashboard/jobs/{jobId}/events?minutes=60
        /// </summary>
        [HttpGet("jobs/{jobId}/events")]
        public async Task<ActionResult<List<RealTimeEventDto>>> GetRecentEvents(Guid jobId, int minutes = 60)
        {
            try
            {
                var events = await _dashboardService.GetRecentEventsAsync(jobId, minutes);
                return Ok(new { success = true, data = events });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting recent events: {ex.Message}");
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        // ============================================================
        // 7. Cache Management
        // ============================================================

        /// <summary>
        /// Get cache statistics
        /// GET /api/v2/dashboard/cache/stats
        /// </summary>
        [HttpGet("cache/stats")]
        [Authorize(Roles = "ADMIN")]
        public async Task<ActionResult<CacheStatsDto>> GetCacheStats()
        {
            try
            {
                var stats = await _dashboardService.GetCacheStatsAsync();
                return Ok(new { success = true, data = stats });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting cache stats: {ex.Message}");
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        /// <summary>
        /// Clear all cache
        /// DELETE /api/v2/dashboard/cache/clear
        /// </summary>
        [HttpDelete("cache/clear")]
        [Authorize(Roles = "ADMIN")]
        public async Task<ActionResult> ClearCache()
        {
            try
            {
                await _dashboardService.ClearCacheAsync();
                return Ok(new { success = true, message = "Cache cleared" });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error clearing cache: {ex.Message}");
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        // ============================================================
        // 8. Bulk Operations
        // ============================================================

        /// <summary>
        /// Refresh dashboard cache
        /// POST /api/v2/dashboard/cache/refresh
        /// </summary>
        [HttpPost("cache/refresh")]
        [Authorize(Roles = "ADMIN")]
        public async Task<ActionResult> RefreshCache(DashboardRefreshRequestDto request)
        {
            try
            {
                await _dashboardService.RefreshDashboardCacheAsync(request);
                return Ok(new { success = true, message = "Dashboard cache refreshed" });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error refreshing cache: {ex.Message}");
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }
    }
}
