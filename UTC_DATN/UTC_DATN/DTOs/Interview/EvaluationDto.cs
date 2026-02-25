namespace UTC_DATN.DTOs.Interview;

public class EvaluationDto
{
    public Guid InterviewId { get; set; }

    public Guid InterviewerId { get; set; }

    public int Score { get; set; }

    public string? Comment { get; set; }

    /// <summary>
    /// Kết quả: "Passed", "Failed", "Consider"
    /// </summary>
    public string Result { get; set; } = null!;

    /// <summary>
    /// Chi tiết đánh giá từng câu hỏi dạng JSON
    /// </summary>
    public string? Details { get; set; }

    public Guid? SubmittedById { get; set; }

    public string? SubmittedByName { get; set; }

    public bool IsBelated { get; set; }
}

public class AiJudgeRequestDto
{
    public string Question { get; set; } = null!;

    public string CandidateAnswer { get; set; } = null!;
}

public class AiJudgeResponseDto
{
    public int Score { get; set; }

    public string Assessment { get; set; } = null!;
}
