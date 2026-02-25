using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using UTC_DATN.Data;
using UTC_DATN.Entities;

namespace UTC_DATN.Controllers;

[ApiController]
[Route("api/candidate/cover-letters")]
[Authorize]
public class CoverLettersController : ControllerBase
{
    private readonly UTC_DATNContext _context;

    public CoverLettersController(UTC_DATNContext context)
    {
        _context = context;
    }

    private Guid? GetCurrentCandidateId()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return null;
        var userGuid = Guid.Parse(userId);
        var candidate = _context.Candidates.FirstOrDefault(c => c.UserId == userGuid && !c.IsDeleted);
        return candidate?.CandidateId;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var candidateId = GetCurrentCandidateId();
        if (candidateId == null) return Unauthorized();

        var list = await _context.CoverLetters
            .Where(c => c.CandidateId == candidateId)
            .OrderByDescending(c => c.IsDefault)
            .ThenByDescending(c => c.UpdatedAt ?? c.CreatedAt)
            .Select(c => new
            {
                coverLetterId = c.CoverLetterId,
                title = c.Title,
                content = c.Content,
                isDefault = c.IsDefault,
                createdAt = c.CreatedAt,
                updatedAt = c.UpdatedAt
            })
            .ToListAsync();

        return Ok(list);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CoverLetterRequest req)
    {
        var candidateId = GetCurrentCandidateId();
        if (candidateId == null) return Unauthorized();

        // Nếu đặt làm mặc định, bỏ mặc định các cái cũ
        if (req.IsDefault)
            await UnsetAllDefaults(candidateId.Value);

        var letter = new CoverLetter
        {
            CoverLetterId = Guid.NewGuid(),
            CandidateId = candidateId.Value,
            Title = req.Title,
            Content = req.Content,
            IsDefault = req.IsDefault,
            CreatedAt = DateTime.UtcNow
        };

        _context.CoverLetters.Add(letter);
        await _context.SaveChangesAsync();

        return Ok(new { coverLetterId = letter.CoverLetterId, message = "Đã tạo lời chào" });
    }


    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] CoverLetterRequest req)
    {
        var candidateId = GetCurrentCandidateId();
        if (candidateId == null) return Unauthorized();

        var letter = await _context.CoverLetters
            .FirstOrDefaultAsync(c => c.CoverLetterId == id && c.CandidateId == candidateId);
        if (letter == null) return NotFound();

        if (req.IsDefault && !letter.IsDefault)
            await UnsetAllDefaults(candidateId.Value);

        letter.Title = req.Title;
        letter.Content = req.Content;
        letter.IsDefault = req.IsDefault;
        letter.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return Ok(new { message = "Đã cập nhật lời chào" });
    }

    /// <summary>Xóa lời chào</summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var candidateId = GetCurrentCandidateId();
        if (candidateId == null) return Unauthorized();

        var letter = await _context.CoverLetters
            .FirstOrDefaultAsync(c => c.CoverLetterId == id && c.CandidateId == candidateId);
        if (letter == null) return NotFound();

        _context.CoverLetters.Remove(letter);
        await _context.SaveChangesAsync();
        return Ok(new { message = "Đã xóa lời chào" });
    }

    /// <summary>Đặt làm mặc định</summary>
    [HttpPatch("{id}/default")]
    public async Task<IActionResult> SetDefault(Guid id)
    {
        var candidateId = GetCurrentCandidateId();
        if (candidateId == null) return Unauthorized();

        await UnsetAllDefaults(candidateId.Value);

        var letter = await _context.CoverLetters
            .FirstOrDefaultAsync(c => c.CoverLetterId == id && c.CandidateId == candidateId);
        if (letter == null) return NotFound();

        letter.IsDefault = true;
        letter.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Ok(new { message = "Đã đặt làm mặc định" });
    }

    private async Task UnsetAllDefaults(Guid candidateId)
    {
        var defaults = await _context.CoverLetters
            .Where(c => c.CandidateId == candidateId && c.IsDefault)
            .ToListAsync();
        foreach (var d in defaults) d.IsDefault = false;
        await _context.SaveChangesAsync();
    }
}

public class CoverLetterRequest
{
    public string Title { get; set; }
    public string Content { get; set; }
    public bool IsDefault { get; set; }
}
