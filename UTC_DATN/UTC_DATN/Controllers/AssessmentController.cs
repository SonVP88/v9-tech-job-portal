namespace UTC_DATN.Controllers;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using UTC_DATN.DTOs.Assessment;
using UTC_DATN.Services.Interfaces;

/// <summary>
/// Assessment & Pre-screening API Controller
/// Handles assessment management, candidate sessions, scoring, and analytics
/// </summary>
[ApiController]
[Route("api/assessments")]
[Authorize]
public class AssessmentController : ControllerBase
{
    private readonly IAssessmentService _assessmentService;
    private readonly ILogger<AssessmentController> _logger;

    public AssessmentController(IAssessmentService assessmentService, ILogger<AssessmentController> logger)
    {
        _assessmentService = assessmentService;
        _logger = logger;
    }

    // ========================================================================
    // ASSESSMENT MANAGEMENT - ADMIN ENDPOINTS
    // ========================================================================

    /// <summary>Get all assessments</summary>
    [HttpGet]
    [Authorize(Roles = "ADMIN,HR")]
    public async Task<IActionResult> GetAllAssessments([FromQuery] bool onlyActive = true)
    {
        try
        {
            var assessments = await _assessmentService.GetAllAssessmentsAsync(onlyActive);
            return Ok(new { success = true, data = assessments });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching assessments");
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    /// <summary>Get assessment details</summary>
    [HttpGet("{assessmentId}")]
    [Authorize(Roles = "ADMIN,HR")]
    public async Task<IActionResult> GetAssessmentDetail(Guid assessmentId)
    {
        try
        {
            var assessment = await _assessmentService.GetAssessmentDetailAsync(assessmentId);
            return Ok(new { success = true, data = assessment });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching assessment detail");
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    /// <summary>Create new assessment</summary>
    [HttpPost]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> CreateAssessment([FromBody] AssessmentCreateDto dto)
    {
        try
        {
            var assessment = await _assessmentService.CreateAssessmentAsync(dto);
            return CreatedAtAction(nameof(GetAssessmentDetail), new { assessmentId = assessment.AssessmentId }, 
                new { success = true, data = assessment });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating assessment");
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    /// <summary>Update assessment</summary>
    [HttpPut("{assessmentId}")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> UpdateAssessment(Guid assessmentId, [FromBody] AssessmentUpdateDto dto)
    {
        try
        {
            var assessment = await _assessmentService.UpdateAssessmentAsync(assessmentId, dto);
            return Ok(new { success = true, data = assessment });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating assessment");
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    /// <summary>Delete assessment</summary>
    [HttpDelete("{assessmentId}")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> DeleteAssessment(Guid assessmentId)
    {
        try
        {
            await _assessmentService.DeleteAssessmentAsync(assessmentId);
            return Ok(new { success = true, message = "Assessment deleted" });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting assessment");
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    /// <summary>Clone existing assessment</summary>
    [HttpPost("{sourceAssessmentId}/clone")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> CloneAssessment(Guid sourceAssessmentId, [FromQuery] string newTitle)
    {
        try
        {
            var cloned = await _assessmentService.CloneAssessmentAsync(sourceAssessmentId, newTitle);
            return Ok(new { success = true, data = cloned });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cloning assessment");
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    // ========================================================================
    // ASSESSMENT SECTION ENDPOINTS
    // ========================================================================

    /// <summary>Get sections for assessment</summary>
    [HttpGet("{assessmentId}/sections")]
    [Authorize(Roles = "ADMIN,HR")]
    public async Task<IActionResult> GetSections(Guid assessmentId)
    {
        try
        {
            var sections = await _assessmentService.GetSectionsAsync(assessmentId);
            return Ok(new { success = true, data = sections });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching sections");
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    /// <summary>Create section in assessment</summary>
    [HttpPost("{assessmentId}/sections")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> CreateSection(Guid assessmentId, [FromBody] AssessmentSectionCreateDto dto)
    {
        try
        {
            var section = await _assessmentService.CreateSectionAsync(assessmentId, dto);
            return Ok(new { success = true, data = section });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating section");
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    /// <summary>Delete section</summary>
    [HttpDelete("sections/{sectionId}")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> DeleteSection(Guid sectionId)
    {
        try
        {
            await _assessmentService.DeleteSectionAsync(sectionId);
            return Ok(new { success = true, message = "Section deleted" });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting section");
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    // ========================================================================
    // QUESTION MANAGEMENT ENDPOINTS
    // ========================================================================

    /// <summary>Create question in section</summary>
    [HttpPost("sections/{sectionId}/questions")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> CreateQuestion(Guid sectionId, [FromBody] QuestionCreateDto dto)
    {
        try
        {
            var question = await _assessmentService.CreateQuestionAsync(sectionId, dto);
            return Ok(new { success = true, data = question });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating question");
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    /// <summary>Update question</summary>
    [HttpPut("questions/{questionId}")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> UpdateQuestion(Guid questionId, [FromBody] QuestionUpdateDto dto)
    {
        try
        {
            var question = await _assessmentService.UpdateQuestionAsync(questionId, dto);
            return Ok(new { success = true, data = question });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating question");
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    /// <summary>Get question details</summary>
    [HttpGet("questions/{questionId}")]
    [Authorize(Roles = "ADMIN,HR")]
    public async Task<IActionResult> GetQuestion(Guid questionId)
    {
        try
        {
            var question = await _assessmentService.GetQuestionAsync(questionId);
            return Ok(new { success = true, data = question });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching question");
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    /// <summary>Delete question</summary>
    [HttpDelete("questions/{questionId}")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> DeleteQuestion(Guid questionId)
    {
        try
        {
            await _assessmentService.DeleteQuestionAsync(questionId);
            return Ok(new { success = true, message = "Question deleted" });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting question");
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    /// <summary>Get questions in section</summary>
    [HttpGet("sections/{sectionId}/questions")]
    [Authorize(Roles = "ADMIN,HR")]
    public async Task<IActionResult> GetSectionQuestions(Guid sectionId)
    {
        try
        {
            var questions = await _assessmentService.GetSectionQuestionsAsync(sectionId);
            return Ok(new { success = true, data = questions });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching section questions");
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    /// <summary>Randomize question order in section</summary>
    [HttpPost("sections/{sectionId}/randomize-questions")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> RandomizeQuestions(Guid sectionId)
    {
        try
        {
            await _assessmentService.RandomizeQuestionsAsync(sectionId);
            return Ok(new { success = true, message = "Questions randomized" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error randomizing questions");
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    /// <summary>Set code execution environment for code question</summary>
    [HttpPost("questions/{questionId}/code-environment")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> SetCodeEnvironment(Guid questionId, [FromBody] CodeExecutionEnvironmentDto dto)
    {
        try
        {
            var env = await _assessmentService.SetCodeEnvironmentAsync(questionId, dto);
            return Ok(new { success = true, data = env });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting code environment");
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    // ========================================================================
    // JOB ASSESSMENT MAPPING
    // ========================================================================

    /// <summary>Assign assessment to job</summary>
    [HttpPost("jobs/{jobId}/assessments")]
    [Authorize(Roles = "ADMIN,HR")]
    public async Task<IActionResult> AssignAssessmentToJob(Guid jobId, [FromBody] JobAssessmentMapCreateDto dto)
    {
        try
        {
            var mapping = await _assessmentService.AssignAssessmentToJobAsync(jobId, dto);
            return Ok(new { success = true, data = mapping });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning assessment to job");
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    /// <summary>Get assessments for job</summary>
    [HttpGet("jobs/{jobId}/assessments")]
    [Authorize(Roles = "ADMIN,HR")]
    public async Task<IActionResult> GetJobAssessments(Guid jobId)
    {
        try
        {
            var assessments = await _assessmentService.GetJobAssessmentsAsync(jobId);
            return Ok(new { success = true, data = assessments });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching job assessments");
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    /// <summary>Remove assessment from job</summary>
    [HttpDelete("job-assessments/{jobAssessmentId}")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> RemoveAssessmentFromJob(Guid jobAssessmentId)
    {
        try
        {
            await _assessmentService.RemoveAssessmentFromJobAsync(jobAssessmentId);
            return Ok(new { success = true, message = "Assessment removed from job" });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing assessment from job");
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    // ========================================================================
    // CANDIDATE ASSESSMENT SESSION ENDPOINTS
    // ========================================================================

    /// <summary>Start assessment session (candidate begins test)</summary>
    [HttpPost("sessions/start")]
    [Authorize(Roles = "CANDIDATE")]
    public async Task<IActionResult> StartSession([FromBody] AssessmentSessionStartDto dto)
    {
        try
        {
            var session = await _assessmentService.StartAssessmentSessionAsync(dto.ApplicationId, dto.AssessmentId);
            return Ok(new { success = true, data = session });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting assessment session");
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    /// <summary>Resume/continue assessment session</summary>
    [HttpGet("sessions/{sessionId}")]
    [Authorize(Roles = "CANDIDATE")]
    public async Task<IActionResult> ResumeSession(Guid sessionId)
    {
        try
        {
            var session = await _assessmentService.ResumeAssessmentSessionAsync(sessionId);
            return Ok(new { success = true, data = session });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resuming assessment session");
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    /// <summary>Submit single answer</summary>
    [HttpPost("sessions/{sessionId}/answers")]
    [Authorize(Roles = "CANDIDATE")]
    public async Task<IActionResult> SubmitAnswer(Guid sessionId, [FromBody] CandidateAnswerSubmitDto dto)
    {
        try
        {
            var answer = await _assessmentService.SubmitAnswerAsync(sessionId, dto);
            return Ok(new { success = true, data = answer });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting answer");
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    /// <summary>Submit all answers and complete assessment</summary>
    [HttpPost("sessions/{sessionId}/submit")]
    [Authorize(Roles = "CANDIDATE")]
    public async Task<IActionResult> SubmitAssessment(Guid sessionId, [FromBody] SessionAnswersSubmitDto dto)
    {
        try
        {
            await _assessmentService.SubmitAllAnswersAsync(sessionId, dto);
            var score = await _assessmentService.CalculateScoreAsync(sessionId);
            return Ok(new { success = true, message = "Assessment submitted", data = score });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting assessment");
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    /// <summary>Get candidate's pending and completed assessments</summary>
    [HttpGet("candidate/{candidateId}/status")]
    [Authorize(Roles = "CANDIDATE")]
    public async Task<IActionResult> GetCandidateAssessmentStatus(Guid candidateId)
    {
        try
        {
            var status = await _assessmentService.GetCandidatePendingAssessmentsAsync(candidateId);
            return Ok(new { success = true, data = status });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching candidate assessment status");
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    // ========================================================================
    // SCORING & EVALUATION ENDPOINTS
    // ========================================================================

    /// <summary>Calculate score for submitted assessment</summary>
    [HttpPost("scores/calculate/{sessionId}")]
    [Authorize(Roles = "ADMIN,HR")]
    public async Task<IActionResult> CalculateScore(Guid sessionId)
    {
        try
        {
            var score = await _assessmentService.CalculateScoreAsync(sessionId);
            return Ok(new { success = true, data = score });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating score");
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    /// <summary>Get full assessment result with review</summary>
    [HttpGet("results/{scoreId}")]
    [Authorize(Roles = "CANDIDATE,ADMIN,HR")]
    public async Task<IActionResult> GetAssessmentResult(Guid scoreId)
    {
        try
        {
            var candidateId = User.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(candidateId)) return Unauthorized();

            var result = await _assessmentService.GetAssessmentResultAsync(scoreId, Guid.Parse(candidateId));
            return Ok(new { success = true, data = result });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching assessment result");
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    /// <summary>Manually adjust score (HR only)</summary>
    [HttpPut("scores/{scoreId}/adjust")]
    [Authorize(Roles = "ADMIN,HR")]
    public async Task<IActionResult> AdjustScore(Guid scoreId, [FromBody] dynamic request)
    {
        try
        {
            decimal adjustment = request.adjustment;
            string notes = request.notes;
            var score = await _assessmentService.AdjustScoreAsync(scoreId, adjustment, notes);
            return Ok(new { success = true, data = score });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adjusting score");
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    /// <summary>Add feedback on assessment</summary>
    [HttpPost("scores/{scoreId}/feedback")]
    [Authorize(Roles = "ADMIN,HR")]
    public async Task<IActionResult> AddFeedback(Guid scoreId, [FromBody] AssessmentFeedbackCreateDto dto)
    {
        try
        {
            var feedback = await _assessmentService.AddFeedbackAsync(scoreId, dto);
            return Ok(new { success = true, data = feedback });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding feedback");
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    /// <summary>Get feedback for assessment result</summary>
    [HttpGet("scores/{scoreId}/feedback")]
    [Authorize(Roles = "ADMIN,HR")]
    public async Task<IActionResult> GetFeedback(Guid scoreId)
    {
        try
        {
            var feedback = await _assessmentService.GetFeedbackAsync(scoreId);
            return Ok(new { success = true, data = feedback });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching feedback");
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    // ========================================================================
    // RANKING & ANALYTICS ENDPOINTS
    // ========================================================================

    /// <summary>Get leaderboard for assessment</summary>
    [HttpGet("{assessmentId}/leaderboard")]
    [Authorize(Roles = "ADMIN,HR")]
    public async Task<IActionResult> GetLeaderboard(Guid assessmentId, [FromQuery] int topCount = 100)
    {
        try
        {
            var leaderboard = await _assessmentService.GetLeaderboardAsync(assessmentId, topCount);
            return Ok(new { success = true, data = leaderboard });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching leaderboard");
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    /// <summary>Get assessment analytics</summary>
    [HttpGet("{assessmentId}/analytics")]
    [Authorize(Roles = "ADMIN,HR")]
    public async Task<IActionResult> GetAnalytics(Guid assessmentId)
    {
        try
        {
            var analytics = await _assessmentService.GetAssessmentAnalyticsAsync(assessmentId);
            return Ok(new { success = true, data = analytics });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching analytics");
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    /// <summary>Recalculate rankings for assessment</summary>
    [HttpPost("{assessmentId}/recalculate-rankings")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> RecalculateRankings(Guid assessmentId)
    {
        try
        {
            await _assessmentService.RecalculateRankingsAsync(assessmentId);
            return Ok(new { success = true, message = "Rankings recalculated" });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recalculating rankings");
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    // ========================================================================
    // BULK OPERATIONS
    // ========================================================================

    /// <summary>Send assessment to multiple candidates</summary>
    [HttpPost("{assessmentId}/bulk-send")]
    [Authorize(Roles = "ADMIN,HR")]
    public async Task<IActionResult> BulkSendAssessment(Guid assessmentId, [FromBody] dynamic request)
    {
        try
        {
            List<Guid> applicationIds = request.applicationIds;
            var sessions = await _assessmentService.BulkSendAssessmentAsync(applicationIds, assessmentId, 60);
            return Ok(new { success = true, message = $"{sessions.Count} assessments sent", data = sessions });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bulk sending assessment");
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    /// <summary>Auto-submit all expired sessions</summary>
    [HttpPost("{assessmentId}/auto-submit-expired")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> AutoSubmitExpired(Guid assessmentId)
    {
        try
        {
            var count = await _assessmentService.AutoSubmitExpiredSessionsBulkAsync(assessmentId);
            return Ok(new { success = true, message = $"{count} sessions auto-submitted" });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error auto-submitting expired sessions");
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    // ========================================================================
    // ADMIN OPERATIONS
    // ========================================================================

    /// <summary>Get all sessions for assessment</summary>
    [HttpGet("{assessmentId}/sessions")]
    [Authorize(Roles = "ADMIN,HR")]
    public async Task<IActionResult> GetAllSessions(Guid assessmentId, [FromQuery] string statusFilter = null)
    {
        try
        {
            var sessions = await _assessmentService.GetAllSessionsAsync(assessmentId, statusFilter);
            return Ok(new { success = true, data = sessions });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching sessions");
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    /// <summary>Delete all sessions for assessment (reset)</summary>
    [HttpDelete("{assessmentId}/reset")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> ResetAssessment(Guid assessmentId)
    {
        try
        {
            await _assessmentService.DeleteAllSessionsAsync(assessmentId);
            return Ok(new { success = true, message = "Assessment reset - all sessions deleted" });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting assessment");
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    // ========================================================================
    // ASSESSMENT TYPES
    // ========================================================================

    /// <summary>Get all assessment types</summary>
    [HttpGet("types/all")]
    [AllowAnonymous]
    public async Task<IActionResult> GetAssessmentTypes()
    {
        try
        {
            var types = await _assessmentService.GetAllAssessmentTypesAsync();
            return Ok(new { success = true, data = types });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching assessment types");
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }
}
