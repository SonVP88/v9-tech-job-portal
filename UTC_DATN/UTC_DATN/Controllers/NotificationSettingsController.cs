using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using UTC_DATN.DTOs;
using UTC_DATN.Services.Interfaces;

namespace UTC_DATN.Controllers
{
    [Route("api/notification-settings")]
    [ApiController]
    [Authorize]
    public class NotificationSettingsController : ControllerBase
    {
        private readonly INotificationSettingsService _service;

        public NotificationSettingsController(INotificationSettingsService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetSettings()
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            var settings = await _service.GetSettingsAsync(userId);
            return Ok(settings);
        }

        [HttpPut]
        public async Task<IActionResult> UpdateSettings([FromBody] NotificationSettingDto dto)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            var updatedSettings = await _service.UpdateSettingsAsync(userId, dto);
            return Ok(updatedSettings);
        }

        private Guid GetUserId()
        {
            var userIdClaim = User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);
            
            // Falback: Try ClaimTypes.NameIdentifier if 'sub' is mapped
            if (userIdClaim == null)
            {
                userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            }

            if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out Guid userId))
            {
                return userId;
            }
            return Guid.Empty;
        }
    }
}
