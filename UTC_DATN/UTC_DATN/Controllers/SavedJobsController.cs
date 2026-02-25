using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using UTC_DATN.Data;
using UTC_DATN.Entities;

namespace UTC_DATN.Controllers;

[ApiController]
[Route("api/candidate/saved-jobs")]
[Authorize]
public class SavedJobsController : ControllerBase
{
    private readonly UTC_DATNContext _context;

    public SavedJobsController(UTC_DATNContext context)
    {
        _context = context;
    }

    private Guid? GetCurrentCandidateId()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return null;

        var userGuid = Guid.Parse(userId);
        var candidate = _context.Candidates
            .FirstOrDefault(c => c.UserId == userGuid && !c.IsDeleted);
        return candidate?.CandidateId;
    }

    /// <summary>
    /// Lấy danh sách job đã lưu của candidate
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetSavedJobs()
    {
        var candidateId = GetCurrentCandidateId();
        if (candidateId == null)
            return Unauthorized(new { message = "Không tìm thấy hồ sơ ứng viên" });

        var savedJobs = await _context.SavedJobs
            .Where(s => s.CandidateId == candidateId)
            .Include(s => s.Job)
                .ThenInclude(j => j.JobSkillMaps)
                    .ThenInclude(m => m.Skill)
            .Include(s => s.Job.CreatedByNavigation)
            .OrderByDescending(s => s.SavedAt)
            .Select(s => new
            {
                savedJobId = s.SavedJobId,
                savedAt = s.SavedAt,
                job = new
                {
                    jobId = s.Job.JobId,
                    title = s.Job.Title,
                    companyName = s.Job.CreatedByNavigation != null ? s.Job.CreatedByNavigation.FullName : "Unknown",
                    location = s.Job.Location,
                    employmentType = s.Job.EmploymentType,
                    salaryMin = s.Job.SalaryMin,
                    salaryMax = s.Job.SalaryMax,
                    currency = s.Job.Currency,
                    status = s.Job.Status,
                    skills = s.Job.JobSkillMaps.Select(m => m.Skill.Name).ToList(),
                    createdAt = s.Job.CreatedAt
                }
            })
            .ToListAsync();

        return Ok(savedJobs);
    }

    /// <summary>
    /// Lưu job (toggle - nếu đã lưu thì bỏ lưu)
    /// </summary>
    [HttpPost("{jobId}")]
    public async Task<IActionResult> ToggleSaveJob(Guid jobId)
    {
        var candidateId = GetCurrentCandidateId();
        if (candidateId == null)
            return Unauthorized(new { message = "Không tìm thấy hồ sơ ứng viên" });

        // Kiểm tra job tồn tại
        var job = await _context.Jobs.FindAsync(jobId);
        if (job == null)
            return NotFound(new { message = "Không tìm thấy việc làm" });

        var existing = await _context.SavedJobs
            .FirstOrDefaultAsync(s => s.CandidateId == candidateId && s.JobId == jobId);

        if (existing != null)
        {
            // Bỏ lưu
            _context.SavedJobs.Remove(existing);
            await _context.SaveChangesAsync();
            return Ok(new { saved = false, message = "Đã bỏ lưu việc làm" });
        }
        else
        {
            // Lưu mới
            var savedJob = new SavedJob
            {
                SavedJobId = Guid.NewGuid(),
                CandidateId = candidateId.Value,
                JobId = jobId,
                SavedAt = DateTime.UtcNow
            };
            _context.SavedJobs.Add(savedJob);
            await _context.SaveChangesAsync();
            return Ok(new { saved = true, message = "Đã lưu việc làm" });
        }
    }

    /// <summary>
    /// Kiểm tra job đã được lưu chưa
    /// </summary>
    [HttpGet("check/{jobId}")]
    public async Task<IActionResult> CheckSaved(Guid jobId)
    {
        var candidateId = GetCurrentCandidateId();
        if (candidateId == null)
            return Ok(new { saved = false });

        var isSaved = await _context.SavedJobs
            .AnyAsync(s => s.CandidateId == candidateId && s.JobId == jobId);

        return Ok(new { saved = isSaved });
    }

    /// <summary>
    /// Lấy danh sách jobId đã lưu (để check trạng thái bookmark)
    /// </summary>
    [HttpGet("ids")]
    public async Task<IActionResult> GetSavedJobIds()
    {
        var candidateId = GetCurrentCandidateId();
        if (candidateId == null)
            return Ok(new List<Guid>());

        var ids = await _context.SavedJobs
            .Where(s => s.CandidateId == candidateId)
            .Select(s => s.JobId)
            .ToListAsync();

        return Ok(ids);
    }
}
