using System;
using System.Collections.Generic;

namespace UTC_DATN.DTOs.AiCodeAssessment
{
    public class CodeChallengeDto
    {
        public Guid ChallengeId { get; set; }
        public Guid JobId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string ProblemStatement { get; set; } = string.Empty;
        public string? InitialCodeText { get; set; }
        public string Language { get; set; } = string.Empty;
        public string Difficulty { get; set; } = string.Empty;
        public int MaxTimeMinutes { get; set; }
    }

    public class CreateCodeChallengeDto
    {
        public Guid JobId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string ProblemStatement { get; set; } = string.Empty;
        public string? InitialCodeText { get; set; }
        public string Language { get; set; } = string.Empty;
        public string Difficulty { get; set; } = "MEDIUM";
        public int MaxTimeMinutes { get; set; } = 60;
        public string? ExpectedOutput { get; set; }
    }

    public class SubmitCodeDto
    {
        public Guid ChallengeId { get; set; }
        public Guid CandidateId { get; set; }
        public Guid ApplicationId { get; set; }
        public string SubmittedCode { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
    }

    public class CodeSubmissionDto
    {
        public Guid SubmissionId { get; set; }
        public Guid ChallengeId { get; set; }
        public Guid ApplicationId { get; set; }
        public Guid CandidateId { get; set; }
        public string SubmittedCode { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime SubmittedAt { get; set; }
    }

    public class AiCodeReviewResponseDto
    {
        public Guid ReviewId { get; set; }
        public Guid SubmissionId { get; set; }
        public int TotalScore { get; set; }
        public string? TimeComplexity { get; set; }
        public string? SpaceComplexity { get; set; }
        public List<string> CodeSmells { get; set; } = new List<string>();
        public List<string> SecurityVulnerabilities { get; set; } = new List<string>();
        public string OverallFeedback { get; set; } = string.Empty;
        public List<string> Suggestions { get; set; } = new List<string>();
        public DateTime ReviewedAt { get; set; }
        public string AiProvider { get; set; } = string.Empty;
    }
}
