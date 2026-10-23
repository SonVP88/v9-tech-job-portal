using System.Text.Json.Serialization;

namespace UTC_DATN.DTOs.Ai;

// =============================================================================
// Input DTO: Thông tin một CV đưa vào batch
// =============================================================================
public sealed class CvInputDto
{
    /// ID duy nhất để track kết quả trong mảng JSON trả về
    public string CandidateId { get; set; } = Guid.NewGuid().ToString();

    /// Nội dung text CV đã được trích xuất bằng PdfPig
    public string CvText { get; set; } = string.Empty;

    /// Tên file gốc (dùng cho fallback display)
    public string FileName { get; set; } = string.Empty;

    /// SHA256 hash của (cvText + jobId) — dùng để kiểm tra cache DB
    public string ContentHash { get; set; } = string.Empty;
}

// =============================================================================
// Output DTO: Kết quả Gemini cho MỘT ứng viên trong batch
// Tương ứng với từng object trong mảng JSON Gemini trả về
// =============================================================================
public sealed class AiCvEvaluationResultDto
{
    [JsonPropertyName("candidateId")]
    public string CandidateId { get; set; } = string.Empty;

    [JsonPropertyName("candidateName")]
    public string CandidateName { get; set; } = "Ứng viên";

    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("phone")]
    public string Phone { get; set; } = string.Empty;

    [JsonPropertyName("aiMatchingScore")]
    public int AiMatchingScore { get; set; }

    [JsonPropertyName("skillsScore")]
    public int SkillsScore { get; set; }

    [JsonPropertyName("experienceScore")]
    public int ExperienceScore { get; set; }

    [JsonPropertyName("healthShiftScore")]
    public int HealthShiftScore { get; set; }

    [JsonPropertyName("extractedSkills")]
    public List<string> ExtractedSkills { get; set; } = new();

    [JsonPropertyName("aiAnalysisSummary")]
    public string AiAnalysisSummary { get; set; } = string.Empty;

    [JsonPropertyName("strengths")]
    public List<string> Strengths { get; set; } = new();

    [JsonPropertyName("weaknesses")]
    public List<string> Weaknesses { get; set; } = new();

    [JsonPropertyName("suggestedRole")]
    public string SuggestedRole { get; set; } = string.Empty;

    [JsonPropertyName("isQualified")]
    public bool IsQualified { get; set; } = true;

    // ---- Metadata (không phải từ Gemini, set bởi service) ----
    public string FileName { get; set; } = string.Empty;
    public bool FromCache { get; set; } = false;
    public bool IsHeuristicFallback { get; set; } = false;
}

// =============================================================================
// Request DTO gửi từ Controller
// =============================================================================
public sealed class BulkCvScreeningRequestDto
{
    public Guid JobId { get; set; }
    public int SkillsWeight { get; set; } = 40;
    public int ExperienceWeight { get; set; } = 30;
    public int HealthShiftWeight { get; set; } = 30;
}

// =============================================================================
// Response trả về cho client
// =============================================================================
public sealed class BulkCvScreeningResponseDto
{
    public int TotalCvsProcessed { get; set; }
    public int CachedCount { get; set; }
    public int ApiCallCount { get; set; }
    public int BatchCount { get; set; }
    public long ElapsedMs { get; set; }
    public List<AiCvEvaluationResultDto> Results { get; set; } = new();
}

// =============================================================================
// DTO lưu cache kết quả vào DB (tránh gọi API trùng)
// =============================================================================
public sealed class CvScreeningCacheDto
{
    public string ContentHash { get; set; } = string.Empty;
    public Guid JobId { get; set; }
    public string ResultJson { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

// =============================================================================
// DTO thông tin Job (để truyền vào batch)
// =============================================================================
public sealed class JobPostingDto
{
    public Guid JobId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string Requirements { get; set; } = string.Empty;
    public int SkillsWeight { get; set; } = 40;
    public int ExperienceWeight { get; set; } = 30;
    public int HealthShiftWeight { get; set; } = 30;
}
