namespace UTC_DATN.Services.Interfaces;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UTC_DATN.DTOs.Assessment;

/// <summary>
/// Service interface for Assessment & Pre-screening module
/// Handles test management, session creation, scoring, and analytics
/// </summary>
public interface IAssessmentService
{
    // ========================================================================
    // ASSESSMENT TYPE MANAGEMENT
    // ========================================================================
    Task<List<AssessmentTypeDto>> GetAllAssessmentTypesAsync();
    Task<AssessmentTypeDto> GetAssessmentTypeAsync(Guid assessmentTypeId);
    Task<AssessmentTypeDto> GetAssessmentTypeByCodeAsync(string typeCode);

    // ========================================================================
    // ASSESSMENT MANAGEMENT
    // ========================================================================
    /// <summary>Create new assessment test</summary>
    Task<AssessmentDto> CreateAssessmentAsync(AssessmentCreateDto dto);
    
    /// <summary>Update existing assessment</summary>
    Task<AssessmentDto> UpdateAssessmentAsync(Guid assessmentId, AssessmentUpdateDto dto);
    
    /// <summary>Get assessment with full details</summary>
    Task<AssessmentDetailDto> GetAssessmentDetailAsync(Guid assessmentId);
    
    /// <summary>Get all active assessments</summary>
    Task<List<AssessmentDto>> GetAllAssessmentsAsync(bool onlyActive = true);
    
    /// <summary>Delete assessment</summary>
    Task<bool> DeleteAssessmentAsync(Guid assessmentId);
    
    /// <summary>Clone existing assessment (duplicate questions, sections)</summary>
    Task<AssessmentDto> CloneAssessmentAsync(Guid sourceAssessmentId, string newTitle);

    // ========================================================================
    // ASSESSMENT SECTION MANAGEMENT
    // ========================================================================
    Task<AssessmentSectionDto> CreateSectionAsync(Guid assessmentId, AssessmentSectionCreateDto dto);
    Task<bool> DeleteSectionAsync(Guid sectionId);
    Task<List<AssessmentSectionDto>> GetSectionsAsync(Guid assessmentId);

    // ========================================================================
    // QUESTION MANAGEMENT
    // ========================================================================
    /// <summary>Add question to assessment section</summary>
    Task<QuestionFullDto> CreateQuestionAsync(Guid sectionId, QuestionCreateDto dto);
    
    /// <summary>Update question</summary>
    Task<QuestionFullDto> UpdateQuestionAsync(Guid questionId, QuestionUpdateDto dto);
    
    /// <summary>Get question with choices/details</summary>
    Task<QuestionFullDto> GetQuestionAsync(Guid questionId);
    
    /// <summary>Delete question</summary>
    Task<bool> DeleteQuestionAsync(Guid questionId);
    
    /// <summary>Get all questions in section</summary>
    Task<List<QuestionFullDto>> GetSectionQuestionsAsync(Guid sectionId);
    
    /// <summary>Randomize question order in section</summary>
    Task<bool> RandomizeQuestionsAsync(Guid sectionId);
    
    /// <summary>Add code execution environment to code question</summary>
    Task<CodeExecutionEnvironmentDto> SetCodeEnvironmentAsync(Guid questionId, CodeExecutionEnvironmentDto dto);

    // ========================================================================
    // JOB ASSESSMENT MAPPING
    // ========================================================================
    /// <summary>Assign assessment to job</summary>
    Task<JobAssessmentMappingDto> AssignAssessmentToJobAsync(Guid jobId, JobAssessmentMapCreateDto dto);
    
    /// <summary>Get assessments assigned to job</summary>
    Task<List<JobAssessmentMappingDto>> GetJobAssessmentsAsync(Guid jobId);
    
    /// <summary>Remove assessment from job</summary>
    Task<bool> RemoveAssessmentFromJobAsync(Guid jobAssessmentId);

    // ========================================================================
    // ASSESSMENT SESSION MANAGEMENT - CANDIDATE PERSPECTIVE
    // ========================================================================
    /// <summary>Start new assessment session (candidate begins test)</summary>
    Task<AssessmentSessionDto> StartAssessmentSessionAsync(Guid applicationId, Guid assessmentId);
    
    /// <summary>Resume existing assessment session</summary>
    Task<AssessmentSessionRespondingDto> ResumeAssessmentSessionAsync(Guid sessionId);
    
    /// <summary>Get assessment session data</summary>
    Task<AssessmentSessionRespondingDto> GetAssessmentSessionAsync(Guid sessionId);
    
    /// <summary>Submit single answer during assessment</summary>
    Task<CandidateAnswerDto> SubmitAnswerAsync(Guid sessionId, CandidateAnswerSubmitDto dto);
    
    /// <summary>Submit all answers at once (bulk)</summary>
    Task<bool> SubmitAllAnswersAsync(Guid sessionId, SessionAnswersSubmitDto dto);
    
    /// <summary>Auto-submit if time expires</summary>
    Task<bool> AutoSubmitExpiredSessionAsync(Guid sessionId);
    
    /// <summary>Get candidate's pending assessments</summary>
    Task<PendingAssessmentsDto> GetCandidatePendingAssessmentsAsync(Guid candidateId);

    // ========================================================================
    // SCORING & EVALUATION
    // ========================================================================
    /// <summary>Calculate score after submission (auto-score)</summary>
    Task<AssessmentScoreDto> CalculateScoreAsync(Guid sessionId);
    
    /// <summary>Get full result details with answer review</summary>
    Task<AssessmentResultDetailDto> GetAssessmentResultAsync(Guid scoreId, Guid candidateId);
    
    /// <summary>Manually adjust score (HR only)</summary>
    Task<AssessmentScoreDto> AdjustScoreAsync(Guid scoreId, decimal adjustment, string notes);
    
    /// <summary>Add feedback on assessment</summary>
    Task<AssessmentFeedbackDto> AddFeedbackAsync(Guid scoreId, AssessmentFeedbackCreateDto dto);
    
    /// <summary>Get feedback for assessment result</summary>
    Task<AssessmentFeedbackDto> GetFeedbackAsync(Guid scoreId);

    // ========================================================================
    // RANKING & ANALYTICS
    // ========================================================================
    /// <summary>Get candidate ranking for specific assessment</summary>
    Task<CandidateRankingDto> GetCandidateRankingAsync(Guid scoreId);
    
    /// <summary>Get leaderboard for assessment</summary>
    Task<AssessmentLeaderboardDto> GetLeaderboardAsync(Guid assessmentId, int topCount = 100);
    
    /// <summary>Get assessment analytics</summary>
    Task<AssessmentAnalyticsDto> GetAssessmentAnalyticsAsync(Guid assessmentId);
    
    /// <summary>Recalculate all rankings for assessment (after manual score changes)</summary>
    Task<bool> RecalculateRankingsAsync(Guid assessmentId);

    // ========================================================================
    // BULK OPERATIONS
    // ========================================================================
    /// <summary>Send assessment to multiple candidates</summary>
    Task<List<AssessmentSessionDto>> BulkSendAssessmentAsync(List<Guid> applicationIds, Guid assessmentId, int durationMinutes);
    
    /// <summary>Auto-submit all expired sessions for assessment</summary>
    Task<int> AutoSubmitExpiredSessionsBulkAsync(Guid assessmentId);

    // ========================================================================
    // ADMIN OPERATIONS
    // ========================================================================
    /// <summary>Get all sessions for assessment (admin view)</summary>
    Task<List<AssessmentSessionDto>> GetAllSessionsAsync(Guid assessmentId, string statusFilter = null);
    
    /// <summary>Delete all sessions for assessment (reset)</summary>
    Task<bool> DeleteAllSessionsAsync(Guid assessmentId);
    
    /// <summary>Export assessment results to CSV</summary>
    Task<byte[]> ExportResultsAsync(Guid assessmentId);
    
    /// <summary>Import questions from CSV/JSON file</summary>
    Task<int> ImportQuestionsAsync(Guid assessmentId, Guid sectionId, byte[] fileContent, string fileExtension);

    // ========================================================================
    // CANDIDATE VIEW ASSESSMENTS
    // ========================================================================
    /// <summary>Get assessments pending for candidate's applications</summary>
    Task<List<CandidateAssessmentStatusDto>> GetCandidateAssessmentStatusAsync(Guid candidateId);
    
    /// <summary>Check if candidate can start assessment (pre-validation)</summary>
    Task<(bool CanStart, string Reason)> CanStartAssessmentAsync(Guid applicationId, Guid assessmentId);
}
