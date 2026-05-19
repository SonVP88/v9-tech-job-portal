using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UTC_DATN.Entities
{
    [Table("CodeChallenge")]
    public class CodeChallenge
    {
        [Key]
        public Guid ChallengeId { get; set; } = Guid.NewGuid();
        
        public Guid JobId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string ProblemStatement { get; set; } = string.Empty;
        public string? InitialCodeText { get; set; }
        public string Language { get; set; } = string.Empty;
        public string Difficulty { get; set; } = string.Empty;
        public int MaxTimeMinutes { get; set; } = 60;
        public string? ExpectedOutput { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;
    }

    [Table("CandidateCodeSubmission")]
    public class CandidateCodeSubmission
    {
        [Key]
        public Guid SubmissionId { get; set; } = Guid.NewGuid();
        
        public Guid ChallengeId { get; set; }
        public Guid CandidateId { get; set; }
        public Guid ApplicationId { get; set; }
        public string SubmittedCode { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
        public string Status { get; set; } = "PENDING_REVIEW";
        
        // Navigation Properties
        [ForeignKey("ChallengeId")]
        public virtual CodeChallenge? Challenge { get; set; }
    }

    [Table("AiCodeReviewReport")]
    public class AiCodeReviewReport
    {
        [Key]
        public Guid ReviewId { get; set; } = Guid.NewGuid();
        
        public Guid SubmissionId { get; set; }
        public string AiProvider { get; set; } = "OPENAI_GPT4O";
        public int TotalScore { get; set; }
        public string? TimeComplexity { get; set; }
        public string? SpaceComplexity { get; set; }
        public string? CodeSmells { get; set; } // JSON
        public string? SecurityVulnerabilities { get; set; } // JSON
        public string OverallFeedback { get; set; } = string.Empty;
        public string? Suggestions { get; set; } // JSON
        public DateTime ReviewedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation Properties
        [ForeignKey("SubmissionId")]
        public virtual CandidateCodeSubmission? Submission { get; set; }
    }
}
