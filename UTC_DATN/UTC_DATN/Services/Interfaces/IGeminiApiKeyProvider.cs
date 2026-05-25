namespace UTC_DATN.Services.Interfaces;

/// <summary>
/// Interface để quản lý nhiều Gemini API keys
/// Hỗ trợ phân tán tải để tránh bị rate limit
/// </summary>
public interface IGeminiApiKeyProvider
{
    /// <summary>
    /// Lấy API key cho module cụ thể
    /// </summary>
    /// <param name="moduleName">Tên module (Chatbot, JD, Interview, CV, Email, Code)</param>
    /// <returns>API key</returns>
    string GetApiKey(string moduleName);

    /// <summary>
    /// Lấy API key tiếp theo bằng round-robin
    /// </summary>
    /// <returns>API key</returns>
    string GetNextApiKey();

    /// <summary>
    /// Lấy tất cả API keys được cấu hình
    /// </summary>
    /// <returns>List of API keys</returns>
    List<string> GetAllApiKeys();

    /// <summary>
    /// Lấy số lượng API keys được cấu hình
    /// </summary>
    /// <returns>Count of API keys</returns>
    int GetApiKeyCount();

    /// <summary>
    /// Báo cáo một request dùng key bị lỗi (status 429/5xx hoặc exception)
    /// </summary>
    void ReportFailure(string apiKey);

    /// <summary>
    /// Báo cáo một request dùng key thành công
    /// </summary>
    void ReportSuccess(string apiKey);

    /// <summary>
    /// Lấy thống kê các API keys (preview, success/failure counts, disabled flag)
    /// </summary>
    List<Models.GeminiKeyStat> GetKeyStats();

    /// <summary>
    /// Vô hiệu hóa Gemini API key theo index
    /// </summary>
    bool DisableKey(int keyIndex);

    /// <summary>
    /// Kích hoạt lại Gemini API key theo index
    /// </summary>
    bool EnableKey(int keyIndex);
}
