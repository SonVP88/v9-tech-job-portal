namespace UTC_DATN.DTOs.Application;

public class ApplicationDto
{
    public Guid ApplicationId { get; set; }
    public Guid CandidateId { get; set; }  // Thêm để gọi API generate-opening
    public string CandidateName { get; set; }
    public string Email { get; set; }
    public string Phone { get; set; }
    public DateTime AppliedAt { get; set; }
    public string CvUrl { get; set; }
    public string Status { get; set; }
    public string? JobTitle { get; set; }  // Thêm để hiển thị trong email
    public Guid JobId { get; set; } // Thêm để xác định job khi view all

    // Thông tin AI Scoring
    public int? MatchScore { get; set; }
    public string? AiExplanation { get; set; }
    public int? SkillMatchPercent { get; set; }
    public int? ExperienceMatchPercent { get; set; }
    public int? EducationMatchPercent { get; set; }
    public int? SoftSkillMatchPercent { get; set; }
    public List<string>? MatchedSkills { get; set; }
    public List<string>? MissingSkills { get; set; }

    // SLA theo stage hiện tại
    public string? CurrentStageCode { get; set; }
    public string? CurrentStageName { get; set; }
    public DateTime? SlaDueAt { get; set; }
    public string? SlaStatus { get; set; }
    public int? SlaOverdueDays { get; set; }
    public int? SlaMaxDays { get; set; }
    public int? SlaWarnBeforeDays { get; set; }

    public string? JobStatus { get; set; }
    public int? JobNumberOfPositions { get; set; }
    public int JobTotalHired { get; set; }

    // ── Industrial ATS Extensions (Dual-Track: PROFESSIONAL & MASS_LABOR) ──
    public string? RecruitmentTrack { get; set; }
    public string? ApplicationSource { get; set; }
    public string? ScoringMethod { get; set; }
    public string? CvFileUrl { get; set; }
    public string? RawResumeText { get; set; }
    public string? OralInterviewChecklistJson { get; set; }
    public Guid? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public string? DepartmentCode { get; set; }

    // ── 7 Business Rules Guard & Warning Badges ────────────────────────────
    public string? CitizenId { get; set; }
    public string? PhoneNormalized { get; set; }
    public int DuplicateCount { get; set; } = 1;
    public bool IsBlacklisted { get; set; }
    public string? BlacklistWarningNote { get; set; }
    public bool IsScannedOrUnparseablePdf { get; set; }
    public string? AiAnalysisSummary { get; set; }
    public int? AppliedScoringVersion { get; set; }
    public int CurrentScoringWeightVersion { get; set; } = 1;
    public bool IsOutdatedScore { get; set; }
    public string? MedicalCheckStatus { get; set; }
    public string? BankAccountNumber { get; set; }
    public string? BankName { get; set; }
    public bool OnboardingDocsCompleted { get; set; }
    public string? ConvertedEmployeeCode { get; set; }
    public DateTime? SlaDeadlineAt { get; set; }
    public bool IsSlaEscalated { get; set; }
    public string? SlaOverrideReason { get; set; }
    public int JobTargetQuantity { get; set; } = 50;
    public int JobHiredCount { get; set; }
    public int JobWaitlistBufferPercent { get; set; } = 15;

    // ── V3 Enterprise Compliance (PII, Interview Score, Human Override, Offer & Transfer) ──
    public DateTime? DateOfBirth { get; set; }
    public bool IsPiiMaskedBeforeAi { get; set; } = true;
    public decimal? InterviewScore { get; set; }
    public bool IsAiOverriddenByHuman { get; set; }
    public string? AiOverrideReason { get; set; }
    public string? OfferStatus { get; set; }
    public decimal? OfferedSalary { get; set; }
    public string? OfferResponseNote { get; set; }
    public Guid? TransferredFromJobId { get; set; }
    public string? TransferredFromJobTitle { get; set; }
    public string? TransferReason { get; set; }
    public bool JobRequireNightShift { get; set; }
    public string? JobRequisitionStatus { get; set; }

    // ── V5 Factory & HR Operations Complete ──
    public bool IsExEmployeeRehire { get; set; }
    public string? PreviousEmployeeCode { get; set; }
    public int InterviewRescheduleCount { get; set; }
    public DateTime? OfferExpiresAt { get; set; }
    public bool IsInTalentPool { get; set; }
    public string? Probation30DayStatus { get; set; }
    public int? InterviewBatchId { get; set; }
    public string? InterviewBatchCode { get; set; }
}

