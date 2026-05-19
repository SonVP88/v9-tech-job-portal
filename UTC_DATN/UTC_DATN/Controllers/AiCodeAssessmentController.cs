using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UTC_DATN.DTOs.AiCodeAssessment;
using UTC_DATN.Services.Interfaces;

namespace UTC_DATN.Controllers
{
    [Route("api/v2/aicode-assessment")]
    [ApiController]
    public class AiCodeAssessmentController : ControllerBase
    {
        private readonly IAiCodeAssessmentService _aiCodeService;

        public AiCodeAssessmentController(IAiCodeAssessmentService aiCodeService)
        {
            _aiCodeService = aiCodeService;
        }

        [HttpPost("challenges")]
        [Authorize(Roles = "ADMIN,HR")]
        public async Task<IActionResult> CreateChallenge([FromBody] CreateCodeChallengeDto request)
        {
            var result = await _aiCodeService.CreateChallengeAsync(request);
            return CreatedAtAction(nameof(GetChallengeById), new { challengeId = result.ChallengeId }, new { success = true, data = result });
        }

        [HttpGet("jobs/{jobId}/challenges")]
        [Authorize]
        public async Task<IActionResult> GetChallengesByJob(Guid jobId)
        {
            var result = await _aiCodeService.GetChallengesByJobAsync(jobId);
            return Ok(new { success = true, data = result });
        }

        [HttpGet("challenges/{challengeId}")]
        [Authorize]
        public async Task<IActionResult> GetChallengeById(Guid challengeId)
        {
            var result = await _aiCodeService.GetChallengeByIdAsync(challengeId);
            if (result == null) return NotFound(new { success = false, message = "Challenge not found" });
            return Ok(new { success = true, data = result });
        }

        [HttpPost("submit")]
        [Authorize(Roles = "CANDIDATE")]
        public async Task<IActionResult> SubmitCode([FromBody] SubmitCodeDto request)
        {
            try
            {
                var result = await _aiCodeService.SubmitCodeAsync(request);
                return Ok(new { success = true, data = result, message = "Code submitted successfully. AI is reviewing..." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("submissions/{submissionId}/review")]
        [Authorize]
        public async Task<IActionResult> GetReview(Guid submissionId)
        {
            try
            {
                var result = await _aiCodeService.GetOrCreateReviewAsync(submissionId);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
    }
}
