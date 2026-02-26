using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using UglyToad.PdfPig;
using UTC_DATN.DTOs.Ai;
using UTC_DATN.Services.Interfaces;

namespace UTC_DATN.Services.Implements;

public class AiMatchingService : IAiMatchingService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AiMatchingService> _logger;
    private readonly UTC_DATN.Data.UTC_DATNContext _dbContext;

    public AiMatchingService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<AiMatchingService> logger,
        UTC_DATN.Data.UTC_DATNContext dbContext)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
        _dbContext = dbContext;
    }

    /// <summary>
    /// Đọc text từ file PDF sử dụng PdfPig
    /// </summary>
    public async Task<string> ExtractTextFromPdfAsync(string filePath)
    {
        try
        {
            _logger.LogInformation("Extracting text from PDF: {FilePath}", filePath);

            if (!System.IO.File.Exists(filePath))
            {
                throw new FileNotFoundException($"PDF file not found: {filePath}");
            }

            var textBuilder = new StringBuilder();

            // Đọc PDF sử dụng PdfPig
            using (var document = PdfDocument.Open(filePath))
            {
                foreach (var page in document.GetPages())
                {
                    var pageText = page.Text;
                    textBuilder.AppendLine(pageText);
                }
            }

            var extractedText = textBuilder.ToString();
            _logger.LogInformation("Extracted {Length} characters from PDF", extractedText.Length);

            return await Task.FromResult(extractedText);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting text from PDF: {FilePath}", filePath);
            throw;
        }
    }

    /// <summary>
    /// Chấm điểm CV bằng Google Gemini AI (Dùng kỹ thuật Document AI Native / Base64)
    /// </summary>
    public async Task<AiScoreResult> ScoreApplicationAsync(string cvFilePath, string jobDescription)
    {
        try
        {
            _logger.LogInformation("Scoring application with AI Native PDF for file: {FilePath}", cvFilePath);

            // Kiểm tra file
            if (!System.IO.File.Exists(cvFilePath))
            {
                throw new FileNotFoundException($"PDF file not found: {cvFilePath}");
            }

            // Lấy API key từ configuration
            var apiKey = _configuration["GeminiAI:ApiKey"];
            if (string.IsNullOrEmpty(apiKey))
            {
                throw new InvalidOperationException("Gemini API key not configured");
            }

            // Đọc file PDF sang chuỗi Base64
            byte[] fileBytes = await System.IO.File.ReadAllBytesAsync(cvFilePath);
            string base64File = Convert.ToBase64String(fileBytes);

            // Tạo prompt cho AI (Không nhét text CV vào nữa)
            var prompt = CreateScoringPrompt(jobDescription);

            // Tạo request body cho Gemini API (Sử dụng inlineData cho file PDF)
            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new object[] // Phải dùng object[] vì có 2 kiểu data khác nhau trong part
                        {
                            new { text = prompt },
                            new { 
                                inlineData = new {
                                    mimeType = "application/pdf",
                                    data = base64File
                                }
                            }
                        }
                    }
                }
            };

            var jsonContent = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            // Gọi Gemini API - Sử dụng Gemini 2.5 Flash
            var apiUrl = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={apiKey}";
            var response = await _httpClient.PostAsync(apiUrl, content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Gemini API error: {StatusCode} - {Error}", response.StatusCode, errorContent);
                throw new HttpRequestException($"Gemini API returned {response.StatusCode}");
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            _logger.LogDebug("Gemini API response: {Response}", responseContent);

            // Parse response
            var aiResult = ParseGeminiResponse(responseContent);

            _logger.LogInformation("AI scoring completed. Score: {Score}", aiResult.Score);
            return aiResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scoring application with AI Document Native");
            throw;
        }
    }

    /// <summary>
    /// Tạo prompt cho AI
    /// </summary>
    private string CreateScoringPrompt(string jobDescription)
    {
        return $@"Bạn là một chuyên gia tuyển dụng chuyên nghiệp. Nhiệm vụ của bạn là đọc file CV (PDF) được đính kèm và đánh giá mức độ phù hợp của nó với mô tả công việc dưới đây.

**MÔ TẢ CÔNG VIỆC:**
{jobDescription}

Hãy đọc kỹ file PDF CV đính kèm. Phân tích và đánh giá CV theo các tiêu chí sau:
1. Kỹ năng kỹ thuật phù hợp
2. Kinh nghiệm làm việc liên quan
3. Trình độ học vấn
4. Các kỹ năng mềm

Trả về kết quả dưới dạng JSON với cấu trúc sau (KHÔNG thêm markdown, chỉ trả về JSON thuần):
{{
  ""score"": <số từ 0-100>,
  ""explanation"": ""<giải thích ngắn gọn về điểm số, không quá 3-4 câu>"",
  ""matchedSkills"": [""<kỹ năng 1>"", ""<kỹ năng 2>"", ...],
  ""missingSkills"": [""<kỹ năng thiếu 1>"", ""<kỹ năng thiếu 2>"", ...]
}}

Lưu ý:
- Score phải là số nguyên từ 0-100
- Explanation phải ngắn gọn, súc tích
- MatchedSkills: Các kỹ năng mà ứng viên có và job yêu cầu
- MissingSkills: Các kỹ năng quan trọng mà job yêu cầu nhưng ứng viên chưa có
- CHỈ TRẢ VỀ CHUỖI JSON, KHÔNG THÊM BẤT KỲ VĂN BẢN NÀO KHÁC BÊN NGOÀI.";
    }

    /// <summary>
    /// Parse response từ Gemini API
    /// </summary>
    private AiScoreResult ParseGeminiResponse(string responseJson)
    {
        try
        {
            using var document = JsonDocument.Parse(responseJson);
            var root = document.RootElement;

            // Lấy text từ response
            var candidates = root.GetProperty("candidates");
            var firstCandidate = candidates[0];
            var content = firstCandidate.GetProperty("content");
            var parts = content.GetProperty("parts");
            var text = parts[0].GetProperty("text").GetString() ?? "";

            _logger.LogDebug("AI generated text: {Text}", text);

            // Clean text (remove markdown code blocks if any)
            text = text.Trim();
            if (text.StartsWith("```json"))
            {
                text = text.Substring(7);
            }
            if (text.StartsWith("```"))
            {
                text = text.Substring(3);
            }
            if (text.EndsWith("```"))
            {
                text = text.Substring(0, text.Length - 3);
            }
            text = text.Trim();

            // Parse JSON result
            using var resultDoc = JsonDocument.Parse(text);
            var resultRoot = resultDoc.RootElement;

            var result = new AiScoreResult
            {
                Score = resultRoot.GetProperty("score").GetInt32(),
                Explanation = resultRoot.GetProperty("explanation").GetString() ?? "",
                MatchedSkills = new List<string>(),
                MissingSkills = new List<string>()
            };

            // Parse matched skills
            if (resultRoot.TryGetProperty("matchedSkills", out var matchedSkills))
            {
                foreach (var skill in matchedSkills.EnumerateArray())
                {
                    result.MatchedSkills.Add(skill.GetString() ?? "");
                }
            }

            // Parse missing skills
            if (resultRoot.TryGetProperty("missingSkills", out var missingSkills))
            {
                foreach (var skill in missingSkills.EnumerateArray())
                {
                    result.MissingSkills.Add(skill.GetString() ?? "");
                }
            }

            // Validate score range
            if (result.Score < 0) result.Score = 0;
            if (result.Score > 100) result.Score = 100;

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing Gemini response: {Response}", responseJson);
            throw new InvalidOperationException("Failed to parse AI response", ex);
        }
    }

    /// <summary>
    /// Tạo nội dung email bằng AI dựa trên trạng thái ứng tuyển
    /// </summary>
    public async Task<string> GenerateEmailContentAsync(string candidateName, string jobTitle, string status, string companyName)
    {
        try
        {
            _logger.LogInformation("📝 Generating email content for candidate: {CandidateName}, Status: {Status}", candidateName, status);

            var apiKey = _configuration["GeminiAI:ApiKey"];
            if (string.IsNullOrEmpty(apiKey))
            {
                throw new InvalidOperationException("Gemini API key not configured");
            }

            // Tạo prompt dựa trên status
            string prompt;
            if (status == "HIRED")
            {
                prompt = $@"Viết một email chúc mừng ứng viên {candidateName} đã trúng tuyển vị trí {jobTitle} tại công ty {companyName}.

Yêu cầu:
- Văn phong chuyên nghiệp, hào hứng, nhiệt tình
- Chúc mừng ứng viên vì đã thể hiện xuất sắc
- Thông báo sẽ liên hệ sớm để hướng dẫn thủ tục tiếp theo
- Yêu cầu xác nhận phản hồi trong vòng 48 giờ
- Độ dài khoảng 150-200 từ
- Chỉ trả về nội dung Body của email dạng HTML đơn giản (dùng thẻ <p>, <strong>, <br>)
- KHÔNG bao gồm thẻ <html>, <head>, <body> bên ngoài
- KHÔNG thêm markdown code blocks";
            }
            else // REJECTED
            {
                prompt = $@"Viết một email từ chối lịch sự gửi đến ứng viên {candidateName} cho vị trí {jobTitle} tại công ty {companyName}.

Yêu cầu:
- Văn phong lịch sự, tinh tế, tôn trọng
- Cảm ơn ứng viên đã quan tâm và dành thời gian ứng tuyển
- Thông báo nhẹ nhàng rằng hồ sơ chưa phù hợp với vị trí lần này
- Khích lệ ứng viên tiếp tục theo dõi các cơ hội khác
- Giữ mối quan hệ tốt đẹp cho tương lai
- Độ dài khoảng 120-150 từ
- Chỉ trả về nội dung Body của email dạng HTML đơn giản (dùng thẻ <p>, <strong>, <br>)
- KHÔNG bao gồm thẻ <html>, <head>, <body> bên ngoài
- KHÔNG thêm markdown code blocks";
            }

            // Tạo request body cho Gemini API
            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = prompt }
                        }
                    }
                }
            };

            var jsonContent = System.Text.Json.JsonSerializer.Serialize(requestBody);
            var content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");

            var apiUrl = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={apiKey}";
            var response = await _httpClient.PostAsync(apiUrl, content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Gemini API error: {StatusCode} - {Error}", response.StatusCode, errorContent);
                throw new HttpRequestException($"Gemini API returned {response.StatusCode}");
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            
            // Parse response để lấy text
            using var document = System.Text.Json.JsonDocument.Parse(responseContent);
            var root = document.RootElement;
            var candidates = root.GetProperty("candidates");
            var firstCandidate = candidates[0];
            var contentProp = firstCandidate.GetProperty("content");
            var parts = contentProp.GetProperty("parts");
            var emailBody = parts[0].GetProperty("text").GetString() ?? "";

            // Clean up markdown nếu có
            emailBody = emailBody.Trim();
            if (emailBody.StartsWith("```html"))
            {
                emailBody = emailBody.Substring(7);
            }
            if (emailBody.StartsWith("```"))
            {
                emailBody = emailBody.Substring(3);
            }
            if (emailBody.EndsWith("```"))
            {
                emailBody = emailBody.Substring(0, emailBody.Length - 3);
            }
            emailBody = emailBody.Trim();

            _logger.LogInformation("✅ Generated email content successfully");
            return emailBody;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating email content");
            
            // Fallback content nếu AI fail
            if (status == "HIRED")
            {
                return $@"<p>Kính gửi <strong>{candidateName}</strong>,</p>
<p>Chúc mừng bạn! Chúng tôi vui mừng thông báo bạn đã trúng tuyển vị trí <strong>{jobTitle}</strong> tại <strong>{companyName}</strong>.</p>
<p>Chúng tôi sẽ liên hệ với bạn trong thời gian sớm nhất để hướng dẫn các bước tiếp theo.</p>
<p>Trân trọng,<br/>{companyName}</p>";
            }
            else
            {
                return $@"<p>Kính gửi <strong>{candidateName}</strong>,</p>
<p>Cảm ơn bạn đã quan tâm và dành thời gian ứng tuyển vị trí <strong>{jobTitle}</strong> tại <strong>{companyName}</strong>.</p>
<p>Sau khi xem xét kỹ lưỡng, chúng tôi nhận thấy hồ sơ của bạn chưa phù hợp với vị trí này vào thời điểm hiện tại.</p>
<p>Chúng tôi khuyến khích bạn tiếp tục theo dõi các cơ hội khác tại công ty.</p>
<p>Trân trọng,<br/>{companyName}</p>";
            }
        }
    }

    /// <summary>
    /// Sinh đoạn mở đầu cho email mời phỏng vấn (Human-in-the-loop)
    /// </summary>
    public async Task<string> GenerateInterviewOpeningAsync(Guid candidateId, Guid jobId)
    {
        try
        {
            _logger.LogInformation("📝 Sinh đoạn mở đầu email mời phỏng vấn cho CandidateId: {CandidateId}, JobId: {JobId}", candidateId, jobId);

            // Lấy thông tin ứng viên
            var candidate = await _dbContext.Candidates
                .Include(c => c.CandidateSkills)
                    .ThenInclude(cs => cs.Skill)
                .Include(c => c.CandidateExperiences)
                .FirstOrDefaultAsync(c => c.CandidateId == candidateId && !c.IsDeleted);

            if (candidate == null)
            {
                throw new InvalidOperationException($"Không tìm thấy ứng viên với ID: {candidateId}");
            }

            // Lấy thông tin Job
            var job = await _dbContext.Jobs
                .FirstOrDefaultAsync(j => j.JobId == jobId && !j.IsDeleted);

            if (job == null)
            {
                throw new InvalidOperationException($"Không tìm thấy công việc với ID: {jobId}");
            }

            // Tạo thông tin điểm mạnh từ kỹ năng và kinh nghiệm
            var skills = candidate.CandidateSkills?.Select(cs => cs.Skill?.Name).Where(s => !string.IsNullOrEmpty(s)).ToList() ?? new List<string>();
            var experiences = candidate.CandidateExperiences?.Select(e => $"{e.Title} tại {e.Company}").ToList() ?? new List<string>();

            var apiKey = _configuration["GeminiAI:ApiKey"];
            if (string.IsNullOrEmpty(apiKey))
            {
                throw new InvalidOperationException("Gemini API key chưa được cấu hình");
            }

            // Tạo prompt theo yêu cầu
            var prompt = $@"Dựa trên hồ sơ ứng viên {candidate.FullName}, hãy viết một đoạn mở đầu email mời phỏng vấn thật ấn tượng.

Thông tin ứng viên:
- Tên: {candidate.FullName}
- Kỹ năng: {(skills.Any() ? string.Join(", ", skills) : "Chưa cập nhật")}
- Kinh nghiệm: {(experiences.Any() ? string.Join("; ", experiences) : "Chưa cập nhật")}
- Headline: {candidate.Headline ?? "Chưa cập nhật"}

Vị trí ứng tuyển: {job.Title}
Mô tả công việc: {job.Description ?? ""}
Yêu cầu: {job.Requirements ?? ""}

Khen ngợi điểm mạnh cụ thể của họ liên quan đến Job {job.Title}.
Giọng văn: Chuyên nghiệp, hào hứng, cá nhân hóa.
Chỉ trả về đoạn văn đó (2-3 câu), không viết tiêu đề hay kết bài, không thêm markdown.";

            // Gọi Gemini API
            var requestBody = new
            {
                contents = new[]
                {
                    new { parts = new[] { new { text = prompt } } }
                }
            };

            var jsonContent = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var apiUrl = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={apiKey}";
            var response = await _httpClient.PostAsync(apiUrl, content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Gemini API error: {StatusCode} - {Error}", response.StatusCode, errorContent);
                throw new HttpRequestException($"Gemini API trả về lỗi {response.StatusCode}");
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            
            // Parse response
            using var document = JsonDocument.Parse(responseContent);
            var root = document.RootElement;
            var candidates = root.GetProperty("candidates");
            var firstCandidate = candidates[0];
            var contentProp = firstCandidate.GetProperty("content");
            var parts = contentProp.GetProperty("parts");
            var openingText = parts[0].GetProperty("text").GetString() ?? "";

            // Clean up
            openingText = openingText.Trim();

            _logger.LogInformation("✅ Đã sinh đoạn mở đầu email thành công");
            return openingText;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi sinh đoạn mở đầu email");
            
            // Fallback content
            var candidate = await _dbContext.Candidates.FindAsync(candidateId);
            var job = await _dbContext.Jobs.FindAsync(jobId);
            return $"Chào {candidate?.FullName ?? "bạn"}, chúng tôi rất ấn tượng với hồ sơ của bạn và muốn mời bạn tham gia phỏng vấn cho vị trí {job?.Title ?? "công việc"}.";
        }
    }

    /// <summary>
    /// Sinh toàn bộ nội dung email từ chối (Human-in-the-loop)
    /// </summary>
    public async Task<string> GenerateRejectionEmailAsync(string candidateName, string jobTitle, List<string> reasons, string note)
    {
        try
        {
            _logger.LogInformation("📝 Sinh email từ chối cho ứng viên: {CandidateName}, Vị trí: {JobTitle}", candidateName, jobTitle);

            var apiKey = _configuration["GeminiAI:ApiKey"];
            if (string.IsNullOrEmpty(apiKey))
            {
                throw new InvalidOperationException("Gemini API key chưa được cấu hình");
            }

            var reasonsText = reasons != null && reasons.Any() 
                ? string.Join(", ", reasons) 
                : "Hồ sơ chưa phù hợp với yêu cầu hiện tại";

            // Tạo prompt theo yêu cầu
            var prompt = $@"Viết email từ chối ứng viên {candidateName} cho vị trí {jobTitle}.

Lý do từ chối: {reasonsText}
Ghi chú thêm từ HR: {(string.IsNullOrEmpty(note) ? "Không có ghi chú thêm" : note)}

Giọng văn: Lịch sự, tiếc nuối, động viên họ ứng tuyển lần sau.
Tuyệt đối không quá gay gắt.

Trả về nội dung email dạng HTML đơn giản (dùng thẻ <p>, <strong>, <br>).
KHÔNG bao gồm thẻ <html>, <head>, <body> bên ngoài.
KHÔNG thêm markdown code blocks.
Độ dài: 150-200 từ.";

            // Gọi Gemini API
            var requestBody = new
            {
                contents = new[]
                {
                    new { parts = new[] { new { text = prompt } } }
                }
            };

            var jsonContent = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var apiUrl = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={apiKey}";
            var response = await _httpClient.PostAsync(apiUrl, content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Gemini API error: {StatusCode} - {Error}", response.StatusCode, errorContent);
                throw new HttpRequestException($"Gemini API trả về lỗi {response.StatusCode}");
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            
            // Parse response
            using var document = JsonDocument.Parse(responseContent);
            var root = document.RootElement;
            var candidates = root.GetProperty("candidates");
            var firstCandidate = candidates[0];
            var contentProp = firstCandidate.GetProperty("content");
            var parts = contentProp.GetProperty("parts");
            var emailBody = parts[0].GetProperty("text").GetString() ?? "";

            // Clean up markdown nếu có
            emailBody = emailBody.Trim();
            if (emailBody.StartsWith("```html"))
            {
                emailBody = emailBody.Substring(7);
            }
            if (emailBody.StartsWith("```"))
            {
                emailBody = emailBody.Substring(3);
            }
            if (emailBody.EndsWith("```"))
            {
                emailBody = emailBody.Substring(0, emailBody.Length - 3);
            }
            emailBody = emailBody.Trim();

            _logger.LogInformation("✅ Đã sinh email từ chối thành công");
            return emailBody;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi sinh email từ chối");
            
            // Fallback content
            return $@"<p>Kính gửi <strong>{candidateName}</strong>,</p>
<p>Cảm ơn bạn đã quan tâm và dành thời gian ứng tuyển vị trí <strong>{jobTitle}</strong>.</p>
<p>Sau khi xem xét kỹ lưỡng, chúng tôi rất tiếc phải thông báo rằng hồ sơ của bạn chưa phù hợp với vị trí này vào thời điểm hiện tại.</p>
<p>Chúng tôi trân trọng sự quan tâm của bạn và khuyến khích bạn tiếp tục theo dõi các cơ hội khác phù hợp hơn trong tương lai.</p>
<p>Chúc bạn nhiều thành công!</p>
<p>Trân trọng,<br/>Phòng Nhân sự</p>";
        }
    }

    /// <summary>
    /// Đánh giá câu trả lời của ứng viên trong phỏng vấn bằng AI (Tech Lead Judge)
    /// </summary>
    public async Task<string> EvaluateAnswerAsync(string question, string candidateAnswer)
    {
        try
        {
            _logger.LogInformation(" Đánh giá câu trả lời bằng AI cho câu hỏi: {Question}", question);

            var apiKey = _configuration["GeminiAI:ApiKey"];
            if (string.IsNullOrEmpty(apiKey))
            {
                throw new InvalidOperationException("Gemini API key chưa được cấu hình");
            }

            // Tạo prompt theo yêu cầu
            var prompt = $@"Bạn là chuyên gia tuyển dụng Tech Lead. Với câu hỏi: ""{question}"" và câu trả lời tóm tắt của ứng viên: ""{candidateAnswer}"", hãy:

1. Đánh giá độ chính xác (thang 10).
2. Chỉ ra điểm thiếu sót/sai lầm (ngắn gọn dưới 3 dòng).

Trả về kết quả dưới dạng JSON với cấu trúc sau (KHÔNG thêm markdown, chỉ trả về JSON thuần):
{{
  ""score"": <số từ 1-10>,
  ""assessment"": ""<nhận xét ngắn gọn dưới 3 dòng>""
}}";

            // Gọi Gemini API
            var requestBody = new
            {
                contents = new[]
                {
                    new { parts = new[] { new { text = prompt } } }
                }
            };

            var jsonContent = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var apiUrl = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={apiKey}";
            var response = await _httpClient.PostAsync(apiUrl, content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Gemini API error: {StatusCode} - {Error}", response.StatusCode, errorContent);
                throw new HttpRequestException($"Gemini API trả về lỗi {response.StatusCode}");
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            
            // Parse response
            using var document = JsonDocument.Parse(responseContent);
            var root = document.RootElement;
            var candidates = root.GetProperty("candidates");
            var firstCandidate = candidates[0];
            var contentProp = firstCandidate.GetProperty("content");
            var parts = contentProp.GetProperty("parts");
            var resultText = parts[0].GetProperty("text").GetString() ?? "";

            // Clean up markdown nếu có
            resultText = resultText.Trim();
            if (resultText.StartsWith("```json"))
            {
                resultText = resultText.Substring(7);
            }
            if (resultText.StartsWith("```"))
            {
                resultText = resultText.Substring(3);
            }
            if (resultText.EndsWith("```"))
            {
                resultText = resultText.Substring(0, resultText.Length - 3);
            }
            resultText = resultText.Trim();

            _logger.LogInformation("✅ Đã đánh giá câu trả lời thành công");
            return resultText;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi đánh giá câu trả lời");
            
            // Fallback content
            return @"{
  ""score"": 5,
  ""assessment"": ""Không thể đánh giá câu trả lời do lỗi hệ thống. Vui lòng thử lại sau.""
}";
        }
    }
}

