using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using UTC_DATN.Models;
using UTC_DATN.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Caching.Memory;

namespace UTC_DATN.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatbotController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<ChatbotController> _logger;
        private readonly IConfiguration _configuration;
        private readonly UTC_DATNContext _context;
        private readonly IMemoryCache _cache;

        public ChatbotController(
            IHttpClientFactory httpClientFactory, 
            ILogger<ChatbotController> logger, 
            IConfiguration configuration,
            UTC_DATNContext context,
            IMemoryCache cache)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _configuration = configuration;
            _context = context;
            _cache = cache;
        }

        [HttpPost("ask")]
        public async Task Ask([FromBody] ChatRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.Message))
            {
                await WriteStreamEvent(Response, "Vui lòng nhập lời nhắn của bạn.");
                return;
            }

            _logger.LogInformation($"[Chatbot] Nhận yêu cầu: {request.Message}");

            try
            {
                // BẮT BUỘC: Đặt Timeout tối đa lên 120s dể phòng trường hợp AI sinh text lên tới 5 trang giấy
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(120));
                var sw = Stopwatch.StartNew();
                var httpClient = _httpClientFactory.CreateClient("GeminiClient");
                
                var apiKey = _configuration["GeminiAI:ApiKey"];
                if (string.IsNullOrEmpty(apiKey))
                {
                    _logger.LogWarning("Gemini API Key is not configured.");
                    await WriteStreamEvent(Response, "Lỗi Server: Chưa cấu hình Gemini API Key.");
                    return;
                }

                // 1. Phân luồng dữ liệu (RAG Keyword Router)
                var msgLower = request.Message.ToLower();
                // Nới lỏng Regex: Chỉ cần chứa từ khóa ngành nghề là kích hoạt RAG để AI luôn có dữ liệu thực tế
                var isJobQuery = Regex.IsMatch(msgLower, @"(việc|job|công việc|intern|thực tập|tuyển|vị trí|developer|lập trình|sinh viên|phần mềm)");
                var isSkillQuery = Regex.IsMatch(msgLower, @"(kỹ năng|skill|ngôn ngữ|framework|công nghệ|yêu cầu)");
                // Nới lỏng Regex: Chấp nhận các câu chào ngắn gọn (dưới 30 ký tự)
                var isSmallTalk = request.Message.Length < 30 && Regex.IsMatch(msgLower, @"(hi|hello|chào|xin chào|ê|alo|bye|tạm biệt|hi there|hey|tư vấn)");

                string systemPrompt = "";

                if (isSmallTalk)
                {
                    Response.ContentType = "text/event-stream";
                    Response.Headers.Add("Cache-Control", "no-cache");
                    Response.Headers.Add("Connection", "keep-alive");

                    var greetingMsg = "Chào bạn! Tôi là V9 Assistant - AI tư vấn tuyển dụng độc quyền của V9 TECH. Tôi có thể giúp bạn tìm kiếm thông tin Việc làm, Kỹ năng IT, hoặc tư vấn kinh nghiệm phỏng vấn. Bạn đang quan tâm đến vị trí nào?";
                    var words = greetingMsg.Split(' ');
                    foreach (var word in words)
                    {
                        var chunkObj = new { text = word + " " };
                        await Response.WriteAsync($"data: {JsonSerializer.Serialize(chunkObj)}\n\n", cts.Token);
                        await Response.Body.FlushAsync(cts.Token);
                        await Task.Delay(30); 
                    }
                    await Response.WriteAsync("data: [DONE]\n\n", cts.Token);
                    await Response.Body.FlushAsync(cts.Token);
                    return; 
                }
                else
                {
                    systemPrompt = $"Bạn là V9 Assistant - AI tư vấn tuyển dụng ĐỘC QUYỀN của V9 TECH. Hôm nay là {DateTime.Now:dd/MM/yyyy HH:mm:ss} (Năm {DateTime.Now.Year}).\nLƯU Ý NGHIÊM NGẶT (GUARDRAILS): Bạn CHỈ ĐƯỢC PHÉP trả lời các câu hỏi liên quan đến: Công việc, Kỹ năng IT, Tuyển dụng, Lời khuyên viết CV/Phỏng vấn. NẾU người dùng hỏi bất cứ thứ gì ngoài luồng (như thời tiết, nấu ăn, toán học, lịch sử, làm thơ, code dạo...), bạn PHẢI TỪ CHỐI LỊCH SỰ và yêu cầu họ hỏi về Tuyển dụng V9 TECH. Bạn KHÔNG bào chữa, CHỈ từ chối.";
                }

                if (isJobQuery)
                {
                    // Lấy Real-time để tránh Admin test đổi status liên tục bị Cache đè
                    var utcNow = DateTime.UtcNow;
                    var activeJobsTitle = await _context.Jobs
                        .Where(j => j.Status == "OPEN" && !j.IsDeleted && (j.Deadline == null || j.Deadline >= utcNow))
                        .OrderByDescending(j => j.CreatedAt)
                        .Take(10) // 10 job mới nhất là đủ, giữ Token nhẹ cho bản 2.5 Flash free tier
                        .Select(j => j.Title)
                        .ToListAsync(cts.Token);
                    
                    _logger.LogInformation($"[Chatbot] Đã tìm thấy {activeJobsTitle.Count} jobs cho RAG.");
                    
                    var jobsString = activeJobsTitle.Any() 
                        ? string.Join(", ", activeJobsTitle) 
                        : "Không có tuyển dụng.";
                    // Chỉ dẫn ngắn gọn nhất có thể để tiết kiệm Token
                    systemPrompt += $"\nJobs: {jobsString}\nKhi giới thiệu job, nhắc user copy tên và tìm trên website. Không trả về URL/link.";
                }
                else if (isSkillQuery)
                {
                    // Cache kỹ năng tận 60 phút vì nó hiếm khi thay đổi
                    if (!_cache.TryGetValue("Chatbot_TopSkills", out List<string>? skills) || skills == null)
                    {
                        // Lấy top các kỹ năng chưa bị khóa
                         skills = await _context.Skills
                            .Where(s => !s.IsDeleted)
                            .Take(15)
                            .Select(s => s.Name)
                            .ToListAsync(cts.Token);

                        var cacheEntryOptions = new MemoryCacheEntryOptions()
                             .SetAbsoluteExpiration(TimeSpan.FromMinutes(60));
                         _cache.Set("Chatbot_TopSkills", skills, cacheEntryOptions);
                    }
                    
                    var skillsStr = skills.Any() ? string.Join(", ", skills) : "Chưa cập nhật kỹ năng.";
                    systemPrompt += $"\nDanh sách Công nghệ/Kỹ năng công ty hay dùng:\n{skillsStr}";
                }

                // 2. Chuẩn bị Payload chuẩn Google (Dùng system_instruction để tách rào chắn ra token riêng)
                var contentsList = new List<object>();
                
                // Thêm lịch sử (2 câu gần nhất - đủ để AI hiểu ngữ cảnh mà không gây nặng Quota)
                if (request.History != null && request.History.Any())
                {
                    foreach (var h in request.History.TakeLast(2))
                    {
                        if (string.IsNullOrWhiteSpace(h.Content)) continue;
                        contentsList.Add(new 
                        { 
                            role = h.Role.ToLower() == "bot" ? "model" : "user", 
                            parts = new[] { new { text = h.Content } } 
                        });
                    }
                }

                // Tin nhắn hiện tại (SẠCH HOÀN TOÀN - không có system prompt nhồi vào)
                contentsList.Add(new 
                { 
                    role = "user", 
                    parts = new[] { new { text = request.Message } } 
                });

                // system_instruction: Tách rào chắn thành token riêng (Cách Google khuyến nghị nhất)
                // Cần dùng Dictionary vì C# không cho phép gach_dưới trong anonymous type identifier
                var payloadObj = new Dictionary<string, object>
                {
                    ["system_instruction"] = new { parts = new[] { new { text = systemPrompt } } },
                    ["contents"] = contentsList.ToArray()
                };
                var payloadJson = JsonSerializer.Serialize(payloadObj);

                // 3. Khởi tạo SSE HTTP Headers trước khi gọi Google
                Response.ContentType = "text/event-stream";
                Response.Headers.Add("Cache-Control", "no-cache");
                await Response.Body.FlushAsync(cts.Token);

                // 4. Hàm thực thi HTTP Call hỗ trợ Streaming
                async Task<HttpResponseMessage> CallGemini(string modelUrl, string json)
                {
                    var req = new HttpRequestMessage(HttpMethod.Post, modelUrl) 
                    { 
                        Content = new StringContent(json, Encoding.UTF8, "application/json") 
                    };
                    return await httpClient.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, cts.Token);
                }

                var url25 = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:streamGenerateContent?alt=sse&key={apiKey}";
                _logger.LogInformation($"[Gemini 2.5] Gọi API... Tokens ước tính: {payloadJson.Length} chars.");
                var response = await CallGemini(url25, payloadJson);
                
                // Retry cho 429 - Đợi 4 giây (đủ thời gian để Google hồi phục Quota)
                if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                {
                    _logger.LogWarning("[Gemini 2.5] 429 Rate Limit. Đợi 4s để thử lại...");
                    response.Dispose();
                    await Task.Delay(4000, cts.Token);
                    response = await CallGemini(url25, payloadJson);
                }

                if (!response.IsSuccessStatusCode)
                {
                    var errBody = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"[Gemini 2.5 Error] {response.StatusCode}: {errBody}");

                    try 
                    { 
                        var logPath = Path.Combine(AppContext.BaseDirectory, "chatbot_api_error.log");
                        await System.IO.File.AppendAllTextAsync(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {response.StatusCode}: {errBody}\n");
                    } 
                    catch { }
                    
                    string errorMsg = response.StatusCode == System.Net.HttpStatusCode.TooManyRequests
                        ? "Hệ thống AI đang bận, bạn vui lòng đợi khoảng 30 giây rồi thử lại nhé!"
                        : "Rất tiếc, hệ thống AI tạm thời không khả dụng. Vui lòng thử lại sau.";

                    response.Dispose();
                    await WriteStreamEvent(Response, errorMsg);
                    return; 
                }
                // 5. Đọc Stream liên tục và đẩy về luồng Console/Angular
                using var responseStream = await response.Content.ReadAsStreamAsync(cts.Token);
                using var reader = new StreamReader(responseStream);

                while (!reader.EndOfStream && !cts.Token.IsCancellationRequested)
                {
                    var line = await reader.ReadLineAsync(cts.Token);
                    if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("data: ")) continue;

                    var jsonPayload = line.Substring("data: ".Length).Trim();
                    if (jsonPayload == "[DONE]") break; // Xong phiên chat

                    try 
                    {
                        using var document = JsonDocument.Parse(jsonPayload);
                        var candidates = document.RootElement.GetProperty("candidates");
                        if (candidates.GetArrayLength() > 0)
                        {
                            var contentProp = candidates[0].GetProperty("content");
                            if (contentProp.TryGetProperty("parts", out var parts) && parts.GetArrayLength() > 0)
                            {
                                if (parts[0].TryGetProperty("text", out var textElement))
                                {
                                    var textChunk = textElement.GetString();
                                    if (!string.IsNullOrEmpty(textChunk))
                                    {
                                        var chunkObj = new { text = textChunk };
                                        var chunkJson = JsonSerializer.Serialize(chunkObj);
                                        await Response.WriteAsync($"data: {chunkJson}\n\n", cts.Token);
                                        await Response.Body.FlushAsync(cts.Token);
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"Bỏ qua chunk JSON dị dạng từ Gemini: {ex.Message}");
                    }
                }
                
                // Kết thúc Stream bình thường, gửi cờ DONE để nhả Frontend Loader
                await Response.WriteAsync("data: [DONE]\n\n", cts.Token);
                await Response.Body.FlushAsync(cts.Token);
            }
            // Khối Fallback 
            catch (Exception ex) when (ex is TaskCanceledException || ex is HttpRequestException)
            {
                _logger.LogWarning(ex, "Gemini API Streaming Timeout/Error.");
                await WriteStreamEvent(Response, "Hệ thống đang quá tải hoặc kết nối bị gián đoạn. Bạn vui lòng thử lại sau nhé!");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi Server khi xử lý Chatbot Streaming.");
                await WriteStreamEvent(Response, "Có lỗi nội bộ xảy ra.");
            }
        }

        // Hàm tiện ích hỗ trợ nhả Text Event Stream một lần nếu bắt lỗi sớm
        private async Task WriteStreamEvent(HttpResponse response, string message)
        {
            if (!response.HasStarted)
            {
                response.ContentType = "text/event-stream";
                response.Headers.Add("Cache-Control", "no-cache");
                response.Headers.Add("Connection", "keep-alive");
            }
            var chunkObj = new { text = message };
            await response.WriteAsync($"data: {JsonSerializer.Serialize(chunkObj)}\n\n");
            await response.WriteAsync("data: [DONE]\n\n");
            await response.Body.FlushAsync();
        }
    }
}
