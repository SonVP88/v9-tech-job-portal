using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UTC_DATN.DTOs.Sla;
using UTC_DATN.Services.Interfaces;
using System.Security.Claims;

namespace UTC_DATN.Controllers;

/// <summary>
/// API Controller for SLA Analytics, Bottleneck Detection, and Pipeline Insights
/// </summary>
[ApiController]
[Route("api/sla-analytics")]
[Authorize]
public class SlaAnalyticsController : ControllerBase
{
    private readonly ISlaAnalyticsService _slaService;
    private readonly ILogger<SlaAnalyticsController> _logger;

    public SlaAnalyticsController(
        ISlaAnalyticsService slaService,
        ILogger<SlaAnalyticsController> logger)
    {
        _slaService = slaService;
        _logger = logger;
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }

    // ==================== SLA Configuration ====================

    /// <summary>
    /// Get all SLA stage configurations
    /// </summary>
    [HttpGet("configurations")]
    public async Task<IActionResult> GetAllConfigurations()
    {
        try
        {
            var configs = await _slaService.GetAllStageConfigurationsAsync();
            return Ok(new { success = true, data = configs });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting SLA configurations");
            return StatusCode(500, new { success = false, message = "Error retrieving configurations" });
        }
    }

    /// <summary>
    /// Get SLA configuration for specific stage
    /// </summary>
    [HttpGet("configurations/{stageCode}")]
    public async Task<IActionResult> GetStageConfiguration(string stageCode)
    {
        try
        {
            var config = await _slaService.GetStageConfigurationAsync(stageCode);
            if (config == null)
                return NotFound(new { success = false, message = "Stage configuration not found" });

            return Ok(new { success = true, data = config });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting configuration for stage: {stageCode}");
            return StatusCode(500, new { success = false, message = "Error retrieving configuration" });
        }
    }

    /// <summary>
    /// Update SLA configuration (Admin only)
    /// </summary>
    [HttpPut("configurations/{stageCode}")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> UpdateConfiguration(string stageCode, [FromBody] UpsertSlaStageConfigDto dto)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(dto.StageName) || dto.SlaMaxDays <= 0)
                return BadRequest(new { success = false, message = "Invalid configuration data" });

            var success = await _slaService.UpdateStageConfigurationAsync(stageCode, dto);
            if (!success)
                return NotFound(new { success = false, message = "Stage configuration not found" });

            return Ok(new { success = true, message = "Configuration updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error updating configuration for stage: {stageCode}");
            return StatusCode(500, new { success = false, message = "Error updating configuration" });
        }
    }

    // ==================== Application SLA Tracking ====================

    /// <summary>
    /// Get SLA tracking for a specific application
    /// </summary>
    [HttpGet("tracking/{applicationId:guid}")]
    public async Task<IActionResult> GetApplicationTracking(Guid applicationId)
    {
        try
        {
            var tracking = await _slaService.GetApplicationSlaTrackingAsync(applicationId);
            if (tracking == null)
                return NotFound(new { success = false, message = "Application tracking not found" });

            return Ok(new { success = true, data = tracking });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting tracking for application: {applicationId}");
            return StatusCode(500, new { success = false, message = "Error retrieving tracking" });
        }
    }

    /// <summary>
    /// Update application to new stage and recalculate SLA
    /// </summary>
    [HttpPost("tracking/{applicationId:guid}/transition")]
    [Authorize(Roles = "HR,ADMIN")]
    public async Task<IActionResult> TransitionApplicationStage(Guid applicationId, [FromBody] TransitionStageRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.NewStageCode))
                return BadRequest(new { success = false, message = "New stage code is required" });

            var success = await _slaService.UpdateApplicationStageAsync(applicationId, request.NewStageCode, request.Notes);
            if (!success)
                return BadRequest(new { success = false, message = "Error updating application stage" });

            var updated = await _slaService.GetApplicationSlaTrackingAsync(applicationId);
            return Ok(new { success = true, message = "Application transitioned successfully", data = updated });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error transitioning application: {applicationId}");
            return StatusCode(500, new { success = false, message = "Error transitioning application" });
        }
    }

    /// <summary>
    /// Move multiple applications to new stage (bulk operation)
    /// </summary>
    [HttpPost("tracking/bulk-transition")]
    [Authorize(Roles = "HR,ADMIN")]
    public async Task<IActionResult> BulkTransition([FromBody] BulkTransitionRequest request)
    {
        try
        {
            if (!request.ApplicationIds?.Any() ?? true)
                return BadRequest(new { success = false, message = "No applications provided" });

            if (string.IsNullOrWhiteSpace(request.NewStageCode))
                return BadRequest(new { success = false, message = "New stage code is required" });

            var userId = GetCurrentUserId();
            var success = await _slaService.BulkTransitionApplicationsAsync(request, userId);
            if (!success)
                return BadRequest(new { success = false, message = "Error in bulk transition" });

            return Ok(new { success = true, message = $"Successfully transitioned {request.ApplicationIds.Count} applications" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in bulk transition");
            return StatusCode(500, new { success = false, message = "Error in bulk transition" });
        }
    }

    // ==================== Bottleneck Analysis ====================

    /// <summary>
    /// Get bottleneck analysis for all stages
    /// </summary>
    [HttpGet("bottleneck/all")]
    public async Task<IActionResult> GetAllBottleneckAnalysis()
    {
        try
        {
            var analyses = await _slaService.GetAllBottleneckAnalysisAsync();
            return Ok(new { success = true, data = analyses });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting bottleneck analysis");
            return StatusCode(500, new { success = false, message = "Error retrieving bottleneck analysis" });
        }
    }

    /// <summary>
    /// Get bottleneck analysis for specific stage
    /// </summary>
    [HttpGet("bottleneck/{stageCode}")]
    public async Task<IActionResult> GetStageBottleneckAnalysis(string stageCode)
    {
        try
        {
            var analysis = await _slaService.GetStageBottleneckAnalysisAsync(stageCode);
            if (analysis == null)
                return NotFound(new { success = false, message = "Bottleneck analysis not found" });

            return Ok(new { success = true, data = analysis });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting bottleneck analysis for stage: {stageCode}");
            return StatusCode(500, new { success = false, message = "Error retrieving bottleneck analysis" });
        }
    }

    /// <summary>
    /// Manually trigger bottleneck analysis calculation
    /// </summary>
    [HttpPost("bottleneck/calculate")]
    [Authorize(Roles = "ADMIN,HR")]
    public async Task<IActionResult> CalculateBottleneckAnalysis()
    {
        try
        {
            var success = await _slaService.CalculateBottleneckAnalysisAsync();
            if (!success)
                return BadRequest(new { success = false, message = "Error calculating bottleneck analysis" });

            return Ok(new { success = true, message = "Bottleneck analysis calculated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating bottleneck analysis");
            return StatusCode(500, new { success = false, message = "Error calculating bottleneck analysis" });
        }
    }

    /// <summary>
    /// Get bottleneck insights with recommendations
    /// </summary>
    [HttpGet("bottleneck/insights")]
    public async Task<IActionResult> GetBottleneckInsights([FromQuery] int topCount = 5)
    {
        try
        {
            var insights = await _slaService.GetBottleneckInsightsAsync(topCount);
            return Ok(new { success = true, data = insights });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting bottleneck insights");
            return StatusCode(500, new { success = false, message = "Error retrieving bottleneck insights" });
        }
    }

    // ==================== SLA Breach Alerts ====================

    /// <summary>
    /// Get all active SLA breach alerts
    /// </summary>
    [HttpGet("alerts/active")]
    public async Task<IActionResult> GetActiveAlerts([FromQuery] int pageSize = 20)
    {
        try
        {
            var alerts = await _slaService.GetActiveAlertsAsync(pageSize);
            var (warningCount, breachCount) = await _slaService.GetUnacknowledgedAlertCountAsync();

            return Ok(new { 
                success = true, 
                data = alerts,
                summary = new { warningCount, breachCount }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active alerts");
            return StatusCode(500, new { success = false, message = "Error retrieving alerts" });
        }
    }

    /// <summary>
    /// Get alerts for specific application
    /// </summary>
    [HttpGet("alerts/application/{applicationId:guid}")]
    public async Task<IActionResult> GetApplicationAlerts(Guid applicationId)
    {
        try
        {
            var alerts = await _slaService.GetApplicationAlertsAsync(applicationId);
            return Ok(new { success = true, data = alerts });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting alerts for application: {applicationId}");
            return StatusCode(500, new { success = false, message = "Error retrieving alerts" });
        }
    }

    /// <summary>
    /// Acknowledge a breach alert
    /// </summary>
    [HttpPut("alerts/{alertId:guid}/acknowledge")]
    [Authorize(Roles = "HR,ADMIN")]
    public async Task<IActionResult> AcknowledgeAlert(Guid alertId, [FromBody] AcknowledgeAlertRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            var success = await _slaService.AcknowledgeAlertAsync(alertId, request.Notes, userId);
            if (!success)
                return NotFound(new { success = false, message = "Alert not found" });

            return Ok(new { success = true, message = "Alert acknowledged successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error acknowledging alert: {alertId}");
            return StatusCode(500, new { success = false, message = "Error acknowledging alert" });
        }
    }

    // ==================== Pipeline Dashboard ====================

    /// <summary>
    /// Get comprehensive pipeline dashboard overview
    /// </summary>
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetPipelineDashboard()
    {
        try
        {
            var dashboard = await _slaService.GetPipelineDashboardAsync();
            return Ok(new { success = true, data = dashboard });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pipeline dashboard");
            return StatusCode(500, new { success = false, message = "Error retrieving dashboard" });
        }
    }

    /// <summary>
    /// Get detailed metrics for each stage
    /// </summary>
    [HttpGet("pipeline/metrics")]
    public async Task<IActionResult> GetPipelineMetrics()
    {
        try
        {
            var metrics = await _slaService.GetPipelineStageMetricsAsync();
            return Ok(new { success = true, data = metrics });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pipeline metrics");
            return StatusCode(500, new { success = false, message = "Error retrieving metrics" });
        }
    }

    /// <summary>
    /// Get applications at risk of SLA breach
    /// </summary>
    [HttpGet("pipeline/at-risk")]
    public async Task<IActionResult> GetApplicationsAtRisk([FromQuery] int daysAhead = 2)
    {
        try
        {
            var atRisk = await _slaService.GetApplicationsAtRiskAsync(daysAhead);
            return Ok(new { success = true, data = atRisk });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting at-risk applications");
            return StatusCode(500, new { success = false, message = "Error retrieving at-risk applications" });
        }
    }

    // ==================== Reporting ====================

    /// <summary>
    /// Generate SLA compliance report
    /// </summary>
    [HttpPost("reports/compliance")]
    [Authorize(Roles = "ADMIN,HR")]
    public async Task<IActionResult> GenerateComplianceReport([FromBody] DateRangeRequest request)
    {
        try
        {
            if (request.FromDate >= request.ToDate)
                return BadRequest(new { success = false, message = "Invalid date range" });

            var report = await _slaService.GenerateComplianceReportAsync(request.FromDate, request.ToDate);
            return Ok(new { success = true, data = report });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating compliance report");
            return StatusCode(500, new { success = false, message = "Error generating report" });
        }
    }

    /// <summary>
    /// Get SLA predictions/forecasts
    /// </summary>
    [HttpGet("predictions")]
    public async Task<IActionResult> GetPredictions([FromQuery] int daysAhead = 7)
    {
        try
        {
            var predictions = await _slaService.GetSlapredictionsAsync(daysAhead);
            return Ok(new { success = true, data = predictions });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting SLA predictions");
            return StatusCode(500, new { success = false, message = "Error retrieving predictions" });
        }
    }

    /// <summary>
    /// Get team performance ranking by SLA compliance
    /// </summary>
    [HttpGet("reports/team-compliance")]
    [Authorize(Roles = "ADMIN,HR")]
    public async Task<IActionResult> GetTeamComplianceRanking()
    {
        try
        {
            var ranking = await _slaService.GetTeamComplianceRankingAsync();
            return Ok(new { success = true, data = ranking });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting team compliance ranking");
            return StatusCode(500, new { success = false, message = "Error retrieving team ranking" });
        }
    }

    // ==================== Utilities ====================

    /// <summary>
    /// Force recalculate SLA for all active applications
    /// </summary>
    [HttpPost("admin/recalculate-all")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> RecalculateAllSlas()
    {
        try
        {
            var success = await _slaService.RecalculateAllSlasAsync();
            if (!success)
                return BadRequest(new { success = false, message = "Error recalculating SLAs" });

            return Ok(new { success = true, message = "All SLAs recalculated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recalculating all SLAs");
            return StatusCode(500, new { success = false, message = "Error recalculating SLAs" });
        }
    }
}

// ==================== Request DTOs ====================

public class TransitionStageRequest
{
    public string NewStageCode { get; set; }
    public string? Notes { get; set; }
}

public class AcknowledgeAlertRequest
{
    public string? Notes { get; set; }
}

public class DateRangeRequest
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
}
