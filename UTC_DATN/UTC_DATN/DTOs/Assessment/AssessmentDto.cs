namespace UTC_DATN.DTOs.Assessment;

using System;
using System.Collections.Generic;

// ============================================================================
// ASSESSMENT TYPE DTOs
// ============================================================================
public class AssessmentTypeDto
{
    public Guid AssessmentTypeId { get; set; }
    public string TypeCode { get; set; }
    public string TypeName { get; set; }
    public string Description { get; set; }
    public string ScoringMethod { get; set; } // AUTO, MANUAL, HYBRID
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

// ============================================================================
// ASSESSMENT DTOs
// ============================================================================
public class AssessmentCreateDto
{
    public Guid AssessmentTypeId { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public int DurationMinutes { get; set; } = 60;
    public decimal PassingScore { get; set; } = 60.0m;
    public bool IsRandomizeQuestions { get; set; } = false;
    public bool ShowResultsToCandidate { get; set; } = true;
    public bool RequiredForJob { get; set; } = true;
}

public class AssessmentUpdateDto
{
    public string Title { get; set; }
    public string Description { get; set; }
    public int DurationMinutes { get; set; }
    public decimal PassingScore { get; set; }
    public bool IsRandomizeQuestions { get; set; }
    public bool ShowResultsToCandidate { get; set; }
    public bool RequiredForJob { get; set; }
    public bool IsActive { get; set; }
}

public class AssessmentDto
{
    public Guid AssessmentId { get; set; }
    public Guid AssessmentTypeId { get; set; }
    public string TypeCode { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public int DurationMinutes { get; set; }
    public decimal PassingScore { get; set; }
    public bool IsRandomizeQuestions { get; set; }
    public bool ShowResultsToCandidate { get; set; }
    public bool RequiredForJob { get; set; }
    public bool IsActive { get; set; }
    public int TotalQuestions { get; set; }
    public decimal TotalPoints { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<AssessmentSectionDto> Sections { get; set; } = new();
}

public class AssessmentDetailDto : AssessmentDto
{
    public List<QuestionFullDto> AllQuestions { get; set; } = new();
    public int AssignedToJobsCount { get; set; }
    public int CompletedByCount { get; set; }
    public decimal AverageScore { get; set; }
}

// ============================================================================
// ASSESSMENT SECTION DTOs
// ============================================================================
public class AssessmentSectionDto
{
    public Guid SectionId { get; set; }
    public Guid AssessmentId { get; set; }
    public string SectionTitle { get; set; }
    public string SectionDescription { get; set; }
    public int SectionOrder { get; set; }
    public int DurationMinutes { get; set; }
    public int QuestionCount { get; set; }
    public decimal TotalPoints { get; set; }
}

public class AssessmentSectionCreateDto
{
    public string SectionTitle { get; set; }
    public string SectionDescription { get; set; }
    public int SectionOrder { get; set; }
    public int DurationMinutes { get; set; }
}

// ============================================================================
// QUESTION DTOs
// ============================================================================
public class QuestionCreateDto
{
    public string QuestionType { get; set; } // MULTIPLE_CHOICE, SHORT_TEXT, LONG_TEXT, CODE_SNIPPET
    public string QuestionText { get; set; }
    public decimal PointsPerQuestion { get; set; } = 1.0m;
    public bool IsRequired { get; set; } = true;
    public string CorrectAnswer { get; set; }
    public string AnswerExplanation { get; set; }
    public string DifficultyLevel { get; set; } = "MEDIUM"; // EASY, MEDIUM, HARD, EXPERT
    public List<string> Tags { get; set; } = new();
    public List<string> Hints { get; set; } = new();
    public List<QuestionChoiceCreateDto> Choices { get; set; } = new();
}

public class QuestionUpdateDto
{
    public string QuestionText { get; set; }
    public decimal PointsPerQuestion { get; set; }
    public string CorrectAnswer { get; set; }
    public string AnswerExplanation { get; set; }
    public string DifficultyLevel { get; set; }
    public List<string> Tags { get; set; }
    public List<string> Hints { get; set; }
}

public class QuestionDto
{
    public Guid QuestionId { get; set; }
    public Guid SectionId { get; set; }
    public string QuestionType { get; set; }
    public string QuestionText { get; set; }
    public int QuestionOrder { get; set; }
    public decimal PointsPerQuestion { get; set; }
    public bool IsRequired { get; set; }
    public string DifficultyLevel { get; set; }
    public List<string> Tags { get; set; } = new();
    public List<string> Hints { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}

public class QuestionFullDto : QuestionDto
{
    public string AnswerExplanation { get; set; }
    public List<QuestionChoiceDto> Choices { get; set; } = new();
}

public class QuestionChoiceDto
{
    public Guid ChoiceId { get; set; }
    public Guid QuestionId { get; set; }
    public int ChoiceOrder { get; set; }
    public string ChoiceText { get; set; }
}

public class QuestionChoiceCreateDto
{
    public int ChoiceOrder { get; set; }
    public string ChoiceText { get; set; }
    public bool IsCorrect { get; set; } = false;
}

// ============================================================================
// ASSESSMENT SESSION DTOs
// ============================================================================
public class AssessmentSessionStartDto
{
    public Guid ApplicationId { get; set; }
    public Guid AssessmentId { get; set; }
}

public class AssessmentSessionDto
{
    public Guid SessionId { get; set; }
    public Guid ApplicationId { get; set; }
    public Guid AssessmentId { get; set; }
    public Guid CandidateId { get; set; }
    public string SessionStatus { get; set; } // PENDING, IN_PROGRESS, SUBMITTED, COMPLETED, EXPIRED
    public DateTime? StartedAt { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public int? DurationMinutes { get; set; }
    public int ResumCount { get; set; }
    public AssessmentDto Assessment { get; set; }
    public List<QuestionDto> Questions { get; set; } = new();
}

public class AssessmentSessionRespondingDto
{
    public Guid SessionId { get; set; }
    public Guid AssessmentId { get; set; }
    public string Title { get; set; }
    public int DurationMinutes { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public int RemainingSeconds { get; set; }
    public List<QuestionForCandidateDto> Questions { get; set; } = new();
    public int CurrentQuestionIndex { get; set; }
    public int TotalQuestions { get; set; }
}

public class QuestionForCandidateDto
{
    public Guid QuestionId { get; set; }
    public string QuestionType { get; set; }
    public string QuestionText { get; set; }
    public decimal PointsPerQuestion { get; set; }
    public bool IsRequired { get; set; }
    public string DifficultyLevel { get; set; }
    public List<string> Hints { get; set; } = new();
    public List<QuestionChoiceDto> Choices { get; set; } = new();
    public bool? IsAnswered { get; set; } // From candidate's perspective
}

// ============================================================================
// ANSWER DTOs
// ============================================================================
public class CandidateAnswerSubmitDto
{
    public Guid QuestionId { get; set; }
    public Guid? ChoiceId { get; set; } // For multiple choice
    public string TextAnswer { get; set; } // For text/code answers
    public string CodeLanguage { get; set; } // For code: CSHARP, PYTHON, JAVA
}

public class SessionAnswersSubmitDto
{
    public Guid SessionId { get; set; }
    public List<CandidateAnswerSubmitDto> Answers { get; set; } = new();
}

public class CandidateAnswerDto
{
    public Guid AnswerId { get; set; }
    public Guid SessionId { get; set; }
    public Guid QuestionId { get; set; }
    public Guid? ChoiceId { get; set; }
    public string TextAnswer { get; set; }
    public string CodeLanguage { get; set; }
    public string CodeExecutionResult { get; set; } // JSON
    public bool? IsCorrect { get; set; }
    public decimal? PointsEarned { get; set; }
    public DateTime SubmittedAt { get; set; }
}

// ============================================================================
// ASSESSMENT SCORE DTOs
// ============================================================================
public class AssessmentScoreDto
{
    public Guid ScoreId { get; set; }
    public Guid SessionId { get; set; }
    public Guid ApplicationId { get; set; }
    public Guid AssessmentId { get; set; }
    public decimal TotalPoints { get; set; }
    public decimal MaxPoints { get; set; }
    public decimal PercentageScore { get; set; }
    public decimal PassingScore { get; set; }
    public bool IsPassed { get; set; }
    public int? CorrectAnswersCount { get; set; }
    public int? TotalQuestionsCount { get; set; }
    public int? TimeSpentSeconds { get; set; }
    public int? Rank { get; set; }
    public int? Percentile { get; set; }
    public DateTime? EvaluatedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AssessmentResultDetailDto
{
    public Guid ScoreId { get; set; }
    public Guid SessionId { get; set; }
    public AssessmentDto Assessment { get; set; }
    public decimal PercentageScore { get; set; }
    public bool IsPassed { get; set; }
    public int CorrectAnswersCount { get; set; }
    public int TotalQuestionsCount { get; set; }
    public decimal TimeSpentMinutes { get; set; }
    public int? Rank { get; set; }
    public int? Percentile { get; set; }
    public Dictionary<string, decimal> SectionScores { get; set; } = new();
    public List<QuestionReviewDto> QuestionReviews { get; set; } = new();
}

public class QuestionReviewDto
{
    public Guid QuestionId { get; set; }
    public string QuestionText { get; set; }
    public string QuestionType { get; set; }
    public decimal MaxPoints { get; set; }
    public decimal PointsEarned { get; set; }
    public bool IsCorrect { get; set; }
    public string CandidateAnswer { get; set; }
    public string CorrectAnswer { get; set; }
    public string AnswerExplanation { get; set; }
}

// ============================================================================
// ASSESSMENT FEEDBACK DTOs
// ============================================================================
public class AssessmentFeedbackCreateDto
{
    public string StrengthNotes { get; set; }
    public string WeaknessProfessionalAreas { get; set; }
    public bool RecommendedFor { get; set; }
    public int FeedbackRating { get; set; } // 1-5
}

public class AssessmentFeedbackDto
{
    public Guid FeedbackId { get; set; }
    public Guid ScoreId { get; set; }
    public string StrengthNotes { get; set; }
    public string WeaknessProfessionalAreas { get; set; }
    public bool RecommendedFor { get; set; }
    public int FeedbackRating { get; set; }
    public string ReviewedByName { get; set; }
    public DateTime ReviewedAt { get; set; }
}

// ============================================================================
// JOB ASSESSMENT MAPPING DTOs
// ============================================================================
public class JobAssessmentMappingDto
{
    public Guid JobAssessmentId { get; set; }
    public Guid JobId { get; set; }
    public Guid AssessmentId { get; set; }
    public string AssessmentTitle { get; set; }
    public bool IsMandatory { get; set; }
    public int SequenceOrder { get; set; }
}

public class JobAssessmentMapCreateDto
{
    public Guid AssessmentId { get; set; }
    public bool IsMandatory { get; set; }
    public int SequenceOrder { get; set; }
}

// ============================================================================
// CODE EXECUTION DTOs
// ============================================================================
public class CodeExecutionResultDto
{
    public bool IsSuccess { get; set; }
    public string Stdout { get; set; }
    public string Stderr { get; set; }
    public int ExecutionTimeMs { get; set; }
    public string ExitCode { get; set; }
}

public class CodeExecutionEnvironmentDto
{
    public Guid EnvironmentId { get; set; }
    public Guid QuestionId { get; set; }
    public string ProgrammingLanguage { get; set; }
    public string StarterCode { get; set; }
    public int TimeoutSeconds { get; set; }
    public int MemoryLimitMb { get; set; }
}

// ============================================================================
// ANALYTICS & REPORTING DTOs
// ============================================================================
public class AssessmentAnalyticsDto
{
    public Guid AssessmentId { get; set; }
    public string Title { get; set; }
    public int TotalCandidatesTaken { get; set; }
    public int TotalCandidatesPassed { get; set; }
    public decimal PassRate { get; set; }
    public decimal AverageScore { get; set; }
    public decimal AverageTimeMinutes { get; set; }
    public Dictionary<string, int> DifficultyDistribution { get; set; } = new();
}

public class CandidateRankingDto
{
    public Guid CandidateId { get; set; }
    public string CandidateName { get; set; }
    public Guid AssessmentId { get; set; }
    public string AssessmentTitle { get; set; }
    public decimal PercentageScore { get; set; }
    public bool IsPassed { get; set; }
    public int Rank { get; set; }
    public int Percentile { get; set; }
    public DateTime CompletedAt { get; set; }
}

public class AssessmentLeaderboardDto
{
    public Guid AssessmentId { get; set; }
    public string AssessmentTitle { get; set; }
    public List<CandidateRankingDto> Rankings { get; set; } = new();
    public int TotalCandidates { get; set; }
}

// ============================================================================
// CANDIDATE ASSESSMENT DASHBOARD DTOs
// ============================================================================
public class CandidateAssessmentStatusDto
{
    public Guid ApplicationId { get; set; }
    public Guid AssessmentId { get; set; }
    public string AssessmentTitle { get; set; }
    public string SessionStatus { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public decimal? ScorePercentage { get; set; }
    public bool? IsPassed { get; set; }
    public bool IsOverdue { get; set; }
}

public class PendingAssessmentsDto
{
    public Guid CandidateId { get; set; }
    public List<CandidateAssessmentStatusDto> PendingAssessments { get; set; } = new();
    public List<CandidateAssessmentStatusDto> CompletedAssessments { get; set; } = new();
    public int TotalPendingCount { get; set; }
    public int TotalCompletedCount { get; set; }
}
