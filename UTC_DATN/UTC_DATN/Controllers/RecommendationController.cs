using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using UTC_DATN.Data;
using UTC_DATN.Services.Interfaces;

namespace UTC_DATN.Controllers;

[Route("api/[controller]")]
[ApiController]
public class RecommendationController : ControllerBase
{
    private readonly IRecommendationService _recommendationService;
    private readonly UTC_DATNContext _context;

    public RecommendationController(IRecommendationService recommendationService, UTC_DATNContext context)
    {
        _recommendationService = recommendationService;
        _context = context;
    }

    [HttpGet("jobs")]
    [Authorize(Roles = "CANDIDATE")]
    public async Task<IActionResult> GetRecommendedJobs([FromQuery] int top = 10)
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out Guid userId))
        {
            return Unauthorized();
        }

        var results = await _recommendationService.GetRecommendedJobsForCandidateAsync(userId, top);
        return Ok(results);
    }

    /// <summary>
    /// Debug endpoint: Xem dữ liệu thực để hiểu tại sao recommendation trả về rỗng
    /// </summary>
    [HttpGet("debug")]
    [Authorize(Roles = "CANDIDATE")]
    public async Task<IActionResult> DebugRecommendation()
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out Guid userId)) return Unauthorized();

        // Lấy kỹ năng của ứng viên
        var candidateSkills = await _context.CandidateSkills
            .Where(cs => cs.Candidate.UserId == userId)
            .Include(cs => cs.Skill)
            .Select(cs => new { cs.SkillId, cs.Skill.Name })
            .ToListAsync();

        // Lấy số jobs OPEN
        var openJobsCount = await _context.Jobs
            .CountAsync(j => !j.IsDeleted && j.Status == "OPEN");

        // Lấy jobs có gán kỹ năng
        var jobsWithSkills = await _context.JobSkillMaps
            .Include(m => m.Job)
            .Include(m => m.Skill)
            .Where(m => !m.Job.IsDeleted && m.Job.Status == "OPEN")
            .Select(m => new { m.Job.Title, SkillId = m.SkillId, SkillName = m.Skill.Name })
            .Take(20)
            .ToListAsync();

        // Tìm matches
        var candidateSkillIds = candidateSkills.Select(s => s.SkillId).ToList();
        var matchingJobSkills = jobsWithSkills.Where(j => candidateSkillIds.Contains(j.SkillId)).ToList();

        return Ok(new
        {
            CandidateSkillsCount = candidateSkills.Count,
            CandidateSkills = candidateSkills,
            OpenJobsCount = openJobsCount,
            JobsWithSkillsCount = jobsWithSkills.Count,
            SampleJobSkills = jobsWithSkills,
            MatchingSkillsFound = matchingJobSkills.Count,
            Matches = matchingJobSkills
        });
    }

    [HttpGet("candidates/{jobId}")]
    [Authorize(Roles = "HR, ADMIN")]
    public async Task<IActionResult> GetRecommendedCandidates(Guid jobId, [FromQuery] int top = 10)
    {
        var results = await _recommendationService.GetRecommendedCandidatesForJobAsync(jobId, top);
        return Ok(results);
    }
}
