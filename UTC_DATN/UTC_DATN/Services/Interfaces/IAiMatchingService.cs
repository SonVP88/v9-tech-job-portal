using UTC_DATN.DTOs.Ai;

namespace UTC_DATN.Services.Interfaces;

public interface IAiMatchingService
{
    /// <summary>
    /// Đọc text từ file PDF
    /// </summary>
    /// <param name="filePath">Đường dẫn tuyệt đối đến file PDF</param>
    /// <returns>Text đã trích xuất từ PDF</returns>
    Task<string> ExtractTextFromPdfAsync(string filePath);

    /// <summary>
    /// Chấm điểm CV dựa trên Job Description bằng AI (Sử dụng Document AI Native)
    /// </summary>
    /// <param name="cvFilePath">Đường dẫn tới file PDF của CV</param>
    /// <param name="jobDescription">Mô tả công việc</param>
    /// <returns>Kết quả chấm điểm</returns>
    Task<AiScoreResult> ScoreApplicationAsync(string cvFilePath, string jobDescription);

    /// <summary>
    /// Tạo nội dung email bằng AI dựa trên trạng thái ứng tuyển
    /// </summary>
    /// <param name="candidateName">Tên ứng viên</param>
    /// <param name="jobTitle">Tên vị trí công việc</param>
    /// <param name="status">Trạng thái (HIRED/REJECTED)</param>
    /// <param name="companyName">Tên công ty</param>
    /// <returns>Nội dung email dạng HTML</returns>
    Task<string> GenerateEmailContentAsync(string candidateName, string jobTitle, string status, string companyName);

    /// <summary>
    /// Sinh đoạn mở đầu cho email mời phỏng vấn (Human-in-the-loop)
    /// </summary>
    /// <param name="candidateId">ID của ứng viên</param>
    /// <param name="jobId">ID của công việc</param>
    /// <returns>Đoạn mở đầu email (2-3 câu)</returns>
    Task<string> GenerateInterviewOpeningAsync(Guid candidateId, Guid jobId);

    /// <summary>
    /// Sinh toàn bộ nội dung email từ chối (Human-in-the-loop)
    /// </summary>
    /// <param name="candidateName">Tên ứng viên</param>
    /// <param name="jobTitle">Vị trí công việc</param>
    /// <param name="reasons">Danh sách lý do từ chối</param>
    /// <param name="note">Ghi chú thêm từ HR</param>
    /// <returns>Nội dung email từ chối dạng HTML</returns>
    Task<string> GenerateRejectionEmailAsync(string candidateName, string jobTitle, List<string> reasons, string note);

    /// <summary>
    /// Đánh giá câu trả lời của ứng viên trong phỏng vấn bằng AI (Tech Lead Judge)
    /// </summary>
    /// <param name="question">Câu hỏi phỏng vấn</param>
    /// <param name="candidateAnswer">Câu trả lời tóm tắt của ứng viên</param>
    /// <returns>Kết quả đánh giá gồm điểm số (thang 10) và nhận xét ngắn gọn</returns>
    Task<string> EvaluateAnswerAsync(string question, string candidateAnswer);
}
