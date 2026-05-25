using Microsoft.AspNetCore.Mvc;
using UTC_DATN.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;

namespace UTC_DATN.Controllers.Admin;

[ApiController]
[Route("api/admin/gemini-keys")]
[Authorize(Roles = "ADMIN,HR")]
public class GeminiKeysController : ControllerBase
{
    private readonly IGeminiApiKeyProvider _provider;

    public GeminiKeysController(IGeminiApiKeyProvider provider)
    {
        _provider = provider;
    }

    [HttpGet]
    public IActionResult Get()
    {
        var stats = _provider.GetKeyStats();
        return Ok(stats);
    }

    [HttpPost("{keyIndex:int}/disable")]
    public IActionResult Disable(int keyIndex)
    {
        var ok = _provider.DisableKey(keyIndex);
        if (!ok)
        {
            return NotFound(new { message = "Không tìm thấy Gemini API key." });
        }

        return Ok(new { message = "Đã vô hiệu hóa Gemini API key." });
    }

    [HttpPost("{keyIndex:int}/enable")]
    public IActionResult Enable(int keyIndex)
    {
        var ok = _provider.EnableKey(keyIndex);
        if (!ok)
        {
            return NotFound(new { message = "Không tìm thấy Gemini API key." });
        }

        return Ok(new { message = "Đã kích hoạt lại Gemini API key." });
    }
}
