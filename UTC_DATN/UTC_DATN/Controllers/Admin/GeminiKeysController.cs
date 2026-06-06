using Microsoft.AspNetCore.Mvc;
using UTC_DATN.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;

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

    [HttpPost("add")]
    public async Task<IActionResult> Add([FromBody] AddGeminiKeyRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Key))
        {
            return BadRequest(new { message = "Gemini API key không được để trống." });
        }

        var ok = await _provider.AddKeyAsync(request.Key);
        if (!ok)
        {
            return BadRequest(new { message = "Thêm key thất bại. Key có thể đã tồn tại hoặc không vượt qua được bước xác thực với Google Gemini API." });
        }

        return Ok(new { message = "Đã xác thực và thêm Gemini API key mới thành công." });
    }
}

public class AddGeminiKeyRequest
{
    public string Key { get; set; } = string.Empty;
}
