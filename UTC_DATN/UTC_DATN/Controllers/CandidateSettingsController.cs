using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using UTC_DATN.Data;
using UTC_DATN.Entities;

namespace UTC_DATN.Controllers;

[ApiController]
[Route("api/candidate/settings")]
[Authorize]
public class CandidateSettingsController : ControllerBase
{
    private readonly UTC_DATNContext _context;

    public CandidateSettingsController(UTC_DATNContext context)
    {
        _context = context;
    }

    private Guid GetCurrentUserId()
        => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // ─── Đổi mật khẩu ──────────────────────────────────────────────────────

    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest req)
    {
        if (req.NewPassword != req.ConfirmPassword)
            return BadRequest(new { message = "Mật khẩu xác nhận không khớp" });

        if (req.NewPassword.Length < 6)
            return BadRequest(new { message = "Mật khẩu phải có ít nhất 6 ký tự" });

        var userId = GetCurrentUserId();
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return NotFound();

        // Verify mật khẩu hiện tại
        if (!BCrypt.Net.BCrypt.Verify(req.CurrentPassword, user.PasswordHash))
            return BadRequest(new { message = "Mật khẩu hiện tại không đúng" });

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.NewPassword);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Đổi mật khẩu thành công" });
    }

    // ─── Cài đặt thông báo ─────────────────────────────────────────────────

    [HttpGet("notifications")]
    public async Task<IActionResult> GetNotificationSettings()
    {
        var userId = GetCurrentUserId();
        var setting = await _context.NotificationSettings
            .FirstOrDefaultAsync(n => n.UserId == userId);

        if (setting == null)
        {
            // Trả về default nếu chưa có
            return Ok(new
            {
                notifyJobOpportunities = true,
                notifyApplicationUpdates = true,
                notifySecurityAlerts = true,
                notifyMarketing = false,
                channelEmail = true,
                channelPush = true
            });
        }

        return Ok(new
        {
            notifyJobOpportunities = setting.NotifyJobOpportunities,
            notifyApplicationUpdates = setting.NotifyApplicationUpdates,
            notifySecurityAlerts = setting.NotifySecurityAlerts,
            notifyMarketing = setting.NotifyMarketing,
            channelEmail = setting.ChannelEmail,
            channelPush = setting.ChannelPush
        });
    }

    [HttpPut("notifications")]
    public async Task<IActionResult> UpdateNotificationSettings([FromBody] NotificationSettingRequest req)
    {
        var userId = GetCurrentUserId();
        var setting = await _context.NotificationSettings
            .FirstOrDefaultAsync(n => n.UserId == userId);

        if (setting == null)
        {
            setting = new NotificationSetting { Id = Guid.NewGuid(), UserId = userId };
            _context.NotificationSettings.Add(setting);
        }

        setting.NotifyJobOpportunities = req.NotifyJobOpportunities;
        setting.NotifyApplicationUpdates = req.NotifyApplicationUpdates;
        setting.NotifySecurityAlerts = req.NotifySecurityAlerts;
        setting.NotifyMarketing = req.NotifyMarketing;
        setting.ChannelEmail = req.ChannelEmail;
        setting.ChannelPush = req.ChannelPush;
        setting.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return Ok(new { message = "Đã cập nhật cài đặt thông báo" });
    }
}

public class ChangePasswordRequest
{
    public string CurrentPassword { get; set; }
    public string NewPassword { get; set; }
    public string ConfirmPassword { get; set; }
}

public class NotificationSettingRequest
{
    public bool NotifyJobOpportunities { get; set; }
    public bool NotifyApplicationUpdates { get; set; }
    public bool NotifySecurityAlerts { get; set; }
    public bool NotifyMarketing { get; set; }
    public bool ChannelEmail { get; set; }
    public bool ChannelPush { get; set; }
}
