using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UTC_DATN.DTOs.Application;
using UTC_DATN.Services.Interfaces;

namespace UTC_DATN.Controllers;

[ApiController]
[Route("api/sla")]
[Authorize(Roles = "HR,ADMIN")]
public class SlaController : ControllerBase
{
    private readonly IApplicationService _applicationService;
    private readonly ILogger<SlaController> _logger;

    public SlaController(IApplicationService applicationService, ILogger<SlaController> logger)
    {
        _applicationService = applicationService;
        _logger = logger;
    }

    [HttpGet("stages")]
    public async Task<IActionResult> GetStageConfigs()
    {
        var configs = await _applicationService.GetSlaStageConfigsAsync();
        return Ok(new { success = true, data = configs });
    }

    [HttpPut("stages/{stageId:guid}")]
    public async Task<IActionResult> UpdateStageConfig(Guid stageId, [FromBody] UpdateSlaStageConfigRequest request)
    {
        try
        {
            var updated = await _applicationService.UpdateSlaStageConfigAsync(stageId, request);
            if (!updated)
            {
                return NotFound(new { success = false, message = "Không tìm thấy stage." });
            }

            return Ok(new { success = true, message = "Cập nhật SLA stage thành công." });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi cập nhật SLA stage {StageId}", stageId);
            return StatusCode(500, new { success = false, message = "Lỗi server khi cập nhật SLA." });
        }
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetSlaDashboard([FromQuery] bool onlyMy = false)
    {
        Guid? recruiterUserId = null;

        if (onlyMy)
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userIdClaim) && Guid.TryParse(userIdClaim, out var parsedUserId))
            {
                recruiterUserId = parsedUserId;
            }
        }

        var data = await _applicationService.GetSlaDashboardAsync(recruiterUserId);
        return Ok(new { success = true, data });
    }
}
