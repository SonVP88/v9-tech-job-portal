using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using UTC_DATN.Services.Interfaces;

namespace UTC_DATN.Services.Implements;

/// <summary>
/// Service quản lý nhiều Gemini API keys
/// Sử dụng round-robin để phân tán tải giữa các keys
/// </summary>
public class GeminiApiKeyProvider : IGeminiApiKeyProvider
{
    private readonly List<ApiKeyInfo> _apiKeys;
    private readonly Dictionary<string, int> _moduleToKeyIndex; // Module -> Key index mapping
    private readonly ILogger<GeminiApiKeyProvider> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private int _roundRobinIndex = 0;
    private readonly object _lock = new object();

    private readonly int _failureThreshold = 5; // số lần fail để disable
    private readonly TimeSpan _failureWindow = TimeSpan.FromMinutes(10);

    private class ApiKeyInfo
    {
        public string Key { get; set; } = string.Empty;
        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }
        public DateTime? LastFailureAt { get; set; }
        public bool Disabled { get; set; }
    }

    /// <summary>
    /// Module mapping configuration
    /// Key: Module name (Chatbot, JD, Interview, CV, Email, Code)
    /// Value: API key index
    /// </summary>
    private static readonly Dictionary<string, int> DefaultModuleMapping = new()
    {
        { "Chatbot", 0 },      // Key 1
        { "JD", 1 },           // Key 2
        { "Interview", 2 },    // Key 3
        { "CV", 3 },           // Key 4
        { "Email", 4 },        // Key 5
        { "Code", 5 }          // Key 6 (nếu có, không bắt buộc)
    };

    public GeminiApiKeyProvider(IConfiguration configuration, ILogger<GeminiApiKeyProvider> logger, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _apiKeys = new List<ApiKeyInfo>();
        _moduleToKeyIndex = new Dictionary<string, int>();

        // Lấy danh sách API keys từ configuration
        var apiKeysSection = configuration.GetSection("GeminiAI:ApiKeys");
        
        if (apiKeysSection.Exists())
        {
            // Nếu cấu hình mảng ApiKeys
            var keys = apiKeysSection.Get<List<string>>();
            if (keys != null && keys.Count > 0)
            {
                foreach (var k in keys)
                {
                    _apiKeys.Add(new ApiKeyInfo { Key = k });
                }
            }
        }
        else
        {
            // Fallback: Kiểm tra single ApiKey (compatibility)
            var singleKey = configuration["GeminiAI:ApiKey"];
            if (!string.IsNullOrEmpty(singleKey))
            {
                _apiKeys.Add(new ApiKeyInfo { Key = singleKey });
            }
        }

        if (_apiKeys.Count == 0)
        {
            _logger.LogWarning("❌ Không có Gemini API keys được cấu hình!");
            throw new InvalidOperationException("Không tìm thấy Gemini API keys trong configuration");
        }

        // Tạo module mapping
        InitializeModuleMapping();

        _logger.LogInformation("✅ GeminiApiKeyProvider khởi tạo thành công với {Count} API keys", _apiKeys.Count);
    }

    private void InitializeModuleMapping()
    {
        // Gán module với API key theo thứ tự round-robin
        var modules = DefaultModuleMapping.Keys.ToList();
        for (int i = 0; i < modules.Count; i++)
        {
            var keyIndex = i % _apiKeys.Count; // Wrap around nếu modules > keys
            _moduleToKeyIndex[modules[i]] = keyIndex;
            _logger.LogInformation("📌 Module '{Module}' sử dụng API Key #{KeyIndex}", modules[i], keyIndex + 1);
        }
    }

    /// <summary>
    /// Lấy API key cho module cụ thể
    /// </summary>
    public string GetApiKey(string moduleName)
    {
        if (string.IsNullOrEmpty(moduleName))
        {
            return GetNextApiKey();
        }

        // Nếu module được mapping, ưu tiên key đó nếu chưa bị disable
        if (_moduleToKeyIndex.TryGetValue(moduleName, out var keyIndex))
        {
            var info = _apiKeys[keyIndex];
            if (!info.Disabled)
            {
                _logger.LogDebug("🔑 Lấy API key cho module '{Module}' (Key #{Index})", moduleName, keyIndex + 1);
                return info.Key;
            }
        }

        // Nếu module không có mapping hoặc key mapped bị disable, lấy key khả dụng bằng round-robin
        _logger.LogWarning("⚠️ Module '{Module}' không có key khả dụng, sử dụng round-robin khả dụng", moduleName);
        return GetNextApiKey();
    }

    /// <summary>
    /// Lấy API key tiếp theo bằng round-robin
    /// Thread-safe implementation
    /// </summary>
    public string GetNextApiKey()
    {
        lock (_lock)
        {
            if (_apiKeys.Count == 0) throw new InvalidOperationException("No API keys configured");

            // Tìm key không disabled, bắt đầu từ _roundRobinIndex
            for (int i = 0; i < _apiKeys.Count; i++)
            {
                var idx = (_roundRobinIndex + i) % _apiKeys.Count;
                if (!_apiKeys[idx].Disabled)
                {
                    var key = _apiKeys[idx].Key;
                    _roundRobinIndex = (idx + 1) % _apiKeys.Count;
                    _logger.LogDebug("🔄 Round-robin: Sử dụng API Key #{Index}", _roundRobinIndex);
                    return key;
                }
            }

            // Nếu tất cả disabled, trả key theo index bất kỳ (fallback)
            var fallback = _apiKeys[_roundRobinIndex].Key;
            _roundRobinIndex = (_roundRobinIndex + 1) % _apiKeys.Count;
            _logger.LogWarning("⚠️ Tất cả API keys bị disabled, trả fallback key #{Index}", _roundRobinIndex);
            return fallback;
        }
    }

    /// <summary>
    /// Lấy tất cả API keys
    /// </summary>
    public List<string> GetAllApiKeys()
    {
        return _apiKeys.Select(i => i.Key).ToList();
    }

    /// <summary>
    /// Lấy số lượng API keys
    /// </summary>
    public int GetApiKeyCount()
    {
        return _apiKeys.Count;
    }

    public void ReportFailure(string apiKey)
    {
        lock (_lock)
        {
            var info = _apiKeys.FirstOrDefault(i => i.Key == apiKey);
            if (info == null) return;
            info.FailureCount++;
            info.LastFailureAt = DateTime.UtcNow;

            // Reset windowed count if last failure outside window
            if (info.LastFailureAt.HasValue)
            {
                // Count logic: if failures exceed threshold within window => disable
                if (info.FailureCount >= _failureThreshold)
                {
                    info.Disabled = true;
                    _logger.LogWarning("🚫 API key disabled after repeated failures: {Key}", TruncateKey(apiKey));
                }
            }
        }
    }

    public void ReportSuccess(string apiKey)
    {
        lock (_lock)
        {
            var info = _apiKeys.FirstOrDefault(i => i.Key == apiKey);
            if (info == null) return;
            info.SuccessCount++;
            // on success, reduce failure count and re-enable if previously disabled
            info.FailureCount = Math.Max(0, info.FailureCount - 1);
            if (info.Disabled && info.FailureCount == 0)
            {
                info.Disabled = false;
                _logger.LogInformation("✅ API key re-enabled after recovery: {Key}", TruncateKey(apiKey));
            }
        }
    }

    public List<UTC_DATN.Services.Models.GeminiKeyStat> GetKeyStats()
    {
        lock (_lock)
        {
            return _apiKeys.Select((i, index) => {
                var assigned = _moduleToKeyIndex
                    .Where(kvp => kvp.Value == index)
                    .Select(kvp => kvp.Key)
                    .ToList();

                return new UTC_DATN.Services.Models.GeminiKeyStat
                {
                    KeyIndex = index,
                    KeyPreview = TruncateKey(i.Key),
                    SuccessCount = i.SuccessCount,
                    FailureCount = i.FailureCount,
                    Disabled = i.Disabled,
                    AssignedModules = assigned
                };
            }).ToList();
        }
    }

    public bool DisableKey(int keyIndex)
    {
        lock (_lock)
        {
            if (keyIndex < 0 || keyIndex >= _apiKeys.Count)
            {
                return false;
            }

            _apiKeys[keyIndex].Disabled = true;
            _logger.LogWarning("🚫 API key #{Index} disabled by admin", keyIndex + 1);
            return true;
        }
    }

    public bool EnableKey(int keyIndex)
    {
        lock (_lock)
        {
            if (keyIndex < 0 || keyIndex >= _apiKeys.Count)
            {
                return false;
            }

            var info = _apiKeys[keyIndex];
            info.Disabled = false;
            info.FailureCount = 0;
            info.LastFailureAt = null;
            _logger.LogInformation("✅ API key #{Index} enabled by admin", keyIndex + 1);
            return true;
        }
    }

    private async Task<bool> ValidateApiKeyAsync(string apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey)) return false;

        try
        {
            _logger.LogInformation("🔍 Đang xác thực API Key với Google Gemini API...");
            var client = _httpClientFactory.CreateClient("GeminiClient");
            
            var apiUrl = $"https://generativelanguage.googleapis.com/v1beta/models?key={apiKey}";
            
            var response = await client.GetAsync(apiUrl);
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("✅ API Key hợp lệ và hoạt động bình thường.");
                return true;
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("⚠️ Xác thực API Key thất bại từ Google API. StatusCode: {StatusCode}, Error: {Error}", response.StatusCode, errorContent);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Gặp lỗi khi cố gắng xác thực API Key qua Google API.");
            return false;
        }
    }

    public async Task<bool> AddKeyAsync(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return false;
        
        var cleanKey = key.Trim();

        // 1. Kiểm tra sơ bộ xem đã tồn tại chưa
        lock (_lock)
        {
            if (_apiKeys.Any(i => i.Key == cleanKey))
            {
                _logger.LogWarning("⚠️ API Key đã tồn tại trong danh sách: {Key}", TruncateKey(cleanKey));
                return false;
            }
        }

        // 2. Xác thực với Google API (ngoài lock block để tránh block luồng hệ thống)
        var isValid = await ValidateApiKeyAsync(cleanKey);
        if (!isValid)
        {
            return false;
        }

        // 3. Tiến hành ghi và lưu trữ trong lock
        lock (_lock)
        {
            // Kiểm tra lại đề phòng race condition
            if (_apiKeys.Any(i => i.Key == cleanKey))
            {
                return false;
            }

            // Thêm vào danh sách runtime
            _apiKeys.Add(new ApiKeyInfo { Key = cleanKey });
            
            // Ánh xạ lại các module theo số lượng key mới
            InitializeModuleMapping();

            // Đồng bộ ghi đè vào file cấu hình appsettings.json
            try
            {
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json");
                if (File.Exists(filePath))
                {
                    var jsonContent = File.ReadAllText(filePath);
                    
                    var dict = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonContent);
                    if (dict != null && dict.TryGetValue("GeminiAI", out var geminiAiObj))
                    {
                        var geminiAiJson = JsonSerializer.Serialize(geminiAiObj);
                        var geminiDict = JsonSerializer.Deserialize<Dictionary<string, object>>(geminiAiJson);
                        if (geminiDict != null)
                        {
                            // Cập nhật mảng ApiKeys mới
                            var currentKeys = _apiKeys.Select(k => k.Key).ToList();
                            geminiDict["ApiKeys"] = currentKeys;
                            dict["GeminiAI"] = geminiDict;

                            var options = new JsonSerializerOptions { WriteIndented = true };
                            var newJson = JsonSerializer.Serialize(dict, options);
                            File.WriteAllText(filePath, newJson);
                            
                            _logger.LogInformation("💾 Tự động ghi và lưu Gemini API Key mới thành công vào appsettings.json");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Lỗi khi tự động lưu Gemini API Key vào file appsettings.json");
            }

            return true;
        }
    }

    private string TruncateKey(string k)
    {
        if (string.IsNullOrEmpty(k)) return "(empty)";
        return k.Length <= 8 ? k : k[..4] + "..." + k[^4..];
    }
}
