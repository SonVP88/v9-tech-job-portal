using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Microsoft.AspNetCore.Hosting;
using QRCoder;
using UTC_DATN.DTOs.Job;
using UTC_DATN.Entities;
using UTC_DATN.Services.Interfaces;

namespace UTC_DATN.Services.Implements;

public class CampaignQrService : ICampaignQrService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private static string? _runtimePublicApiBase;
    private static string? _runtimePublicClientBase;
    private static string? _runtimeWebhookSecret;

    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<CampaignQrService> _logger;

    public CampaignQrService(
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        IWebHostEnvironment env,
        ILogger<CampaignQrService> logger)
    {
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
        _env = env;
        _logger = logger;
    }

    public (string PublicApiBaseUrl, string PublicClientBaseUrl, string WebhookSecret) GetPublicConfig()
    {
        var apiBase = (!string.IsNullOrWhiteSpace(_runtimePublicApiBase)
            ? _runtimePublicApiBase
            : _configuration["GoogleIntegration:PublicApiBaseUrl"] ?? "http://localhost:5222").TrimEnd('/');

        var clientBase = (!string.IsNullOrWhiteSpace(_runtimePublicClientBase)
            ? _runtimePublicClientBase
            : _configuration["GoogleIntegration:PublicClientBaseUrl"] ?? "http://localhost:4200").TrimEnd('/');

        var secret = !string.IsNullOrWhiteSpace(_runtimeWebhookSecret)
            ? _runtimeWebhookSecret
            : _configuration["GoogleIntegration:WebhookSecret"] ?? "ats_google_webhook_secret_2026";

        return (apiBase, clientBase, secret);
    }

    public void SetRuntimePublicConfig(string? publicApiBaseUrl, string? publicClientBaseUrl, string? webhookSecret = null)
    {
        if (!string.IsNullOrWhiteSpace(publicApiBaseUrl))
        {
            _runtimePublicApiBase = publicApiBaseUrl.Trim().TrimEnd('/');
        }
        if (!string.IsNullOrWhiteSpace(publicClientBaseUrl))
        {
            _runtimePublicClientBase = publicClientBaseUrl.Trim().TrimEnd('/');
        }
        if (!string.IsNullOrWhiteSpace(webhookSecret))
        {
            _runtimeWebhookSecret = webhookSecret.Trim();
        }
    }

    public async Task<CampaignQrDto> CreateForJobAsync(
        Job job,
        bool forceRecreate = false,
        string? customApiBase = null,
        string? customClientBase = null)
    {
        if (!string.IsNullOrWhiteSpace(customApiBase) || !string.IsNullOrWhiteSpace(customClientBase))
        {
            SetRuntimePublicConfig(customApiBase, customClientBase);
        }

        var (apiBase, clientBase, secret) = GetPublicConfig();

        // Chỉ sử dụng đường dẫn Google Form được gán trực tiếp, bỏ cơ chế tự động tạo form nội bộ ATS
        string? existingUrl = null;
        if (!string.IsNullOrWhiteSpace(job.GoogleFormPublicUrl)
            && !job.GoogleFormPublicUrl.Contains("/apply/", StringComparison.OrdinalIgnoreCase))
        {
            existingUrl = job.GoogleFormPublicUrl.Trim();
        }
        else if (!string.IsNullOrWhiteSpace(job.GoogleFormUrl)
            && !job.GoogleFormUrl.Contains("/apply/", StringComparison.OrdinalIgnoreCase))
        {
            existingUrl = job.GoogleFormUrl.Trim();
        }

        string formUrl = existingUrl ?? string.Empty;
        string source = "GOOGLE_FORM";
        string? formId = job.GoogleFormId;
        string? editUrl = job.GoogleFormEditUrl;
        string qrCodeDataUrl = string.Empty;
        string? relativePath = job.QrCodeImagePath;

        if (!string.IsNullOrWhiteSpace(formUrl))
        {
            var png = GeneratePngBytes(formUrl);
            relativePath = await SaveQrPngAsync(job.JobId, png);
            qrCodeDataUrl = ToDataUrl(png);
        }

        job.CampaignFormSource = source;
        job.GoogleFormId = formId;
        job.GoogleFormPublicUrl = string.IsNullOrWhiteSpace(formUrl) ? null : formUrl;
        job.GoogleFormUrl = string.IsNullOrWhiteSpace(formUrl) ? null : formUrl;
        job.GoogleFormEditUrl = editUrl;
        job.QrCodeImagePath = relativePath;

        var webhookUrl = $"{apiBase}/api/applications/webhook-google-form";
        var isPublic = !apiBase.Contains("localhost", StringComparison.OrdinalIgnoreCase)
                       && !apiBase.Contains("127.0.0.1", StringComparison.OrdinalIgnoreCase);

        return new CampaignQrDto
        {
            JobId = job.JobId,
            JobTitle = job.Title,
            Source = source,
            GoogleFormId = formId,
            FormUrl = formUrl,
            FormEditUrl = editUrl,
            QrCodeDataUrl = qrCodeDataUrl,
            QrCodePath = relativePath ?? string.Empty,
            Warning = null,
            AppsScript = BuildAppsScript(job.JobId, apiBase, secret),
            PublicApiBaseUrl = apiBase,
            PublicClientBaseUrl = clientBase,
            WebhookUrl = webhookUrl,
            WebhookSecret = secret,
            IsUsingPublicUrl = isPublic
        };
    }

    public string GeneratePngDataUrl(string content) => ToDataUrl(GeneratePngBytes(content));

    public byte[] GeneratePngBytes(string content)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);
        var pngQr = new PngByteQRCode(data);
        return pngQr.GetGraphic(20);
    }

    private static string ToDataUrl(byte[] png) =>
        "data:image/png;base64," + Convert.ToBase64String(png);

    private async Task<string> SaveQrPngAsync(Guid jobId, byte[] png)
    {
        var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
        var folder = Path.Combine(webRoot, "uploads", "qrcodes");
        Directory.CreateDirectory(folder);
        var fileName = $"{jobId}.png";
        var fullPath = Path.Combine(folder, fileName);
        await System.IO.File.WriteAllBytesAsync(fullPath, png);
        return $"/uploads/qrcodes/{fileName}";
    }

    private async Task<(string FormId, string PublicUrl, string EditUrl)?> TryCreateGoogleFormAsync(Job job)
    {
        var accessToken = await GetGoogleAccessTokenAsync();
        if (string.IsNullOrWhiteSpace(accessToken))
            return null;

        var client = _httpClientFactory.CreateClient("GoogleApis");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var createPayload = new
        {
            info = new
            {
                title = $"Ứng tuyển: {job.Title}",
                documentTitle = $"ATS_{job.Code}_{job.Title}"
            }
        };

        using var createResp = await client.PostAsync(
            "https://forms.googleapis.com/v1/forms",
            ToJsonContent(createPayload));
        var createBody = await createResp.Content.ReadAsStringAsync();
        if (!createResp.IsSuccessStatusCode)
            throw new InvalidOperationException($"Google Forms API tạo form thất bại: {createBody}");

        using var createDoc = JsonDocument.Parse(createBody);
        var formId = createDoc.RootElement.GetProperty("formId").GetString()
            ?? throw new InvalidOperationException("Google Forms không trả về formId.");

        var batchPayload = new
        {
            includeFormInResponse = true,
            requests = new object[]
            {
                new
                {
                    updateFormInfo = new
                    {
                        info = new
                        {
                            description =
                                $"Chiến dịch ATS {job.Code} — {job.Title}\n" +
                                $"Mã JobId: {job.JobId}\n" +
                                "Công nhân quét QR, điền form này. Không sửa dòng mã JobId."
                        },
                        updateMask = "description"
                    }
                },
                TextQuestion("Họ và tên", 0, required: true),
                TextQuestion("Số điện thoại", 1, required: true),
                TextQuestion("Năm sinh", 2, required: false),
                TextQuestion("Quê quán / Thường trú", 3, required: false),
                TextQuestion("Kinh nghiệm làm việc", 4, required: false, paragraph: true),
                ChoiceQuestion("Sẵn sàng làm việc xoay 3 ca / tăng ca", 5, new[] { "Có", "Không" }),
                TextQuestion("Email (nếu có)", 6, required: false)
            }
        };

        using var batchResp = await client.PostAsync(
            $"https://forms.googleapis.com/v1/forms/{formId}:batchUpdate",
            ToJsonContent(batchPayload));
        var batchBody = await batchResp.Content.ReadAsStringAsync();
        if (!batchResp.IsSuccessStatusCode)
            throw new InvalidOperationException($"Google Forms API thêm câu hỏi thất bại: {batchBody}");

        using var getResp = await client.GetAsync($"https://forms.googleapis.com/v1/forms/{formId}");
        var getBody = await getResp.Content.ReadAsStringAsync();
        if (!getResp.IsSuccessStatusCode)
            throw new InvalidOperationException($"Google Forms API đọc form thất bại: {getBody}");

        using var getDoc = JsonDocument.Parse(getBody);
        var responderUri = getDoc.RootElement.TryGetProperty("responderUri", out var ru)
            ? ru.GetString()
            : null;
        if (string.IsNullOrWhiteSpace(responderUri))
            responderUri = $"https://docs.google.com/forms/d/{formId}/viewform";

        await TryShareFormAsync(client, formId);

        var editUrl = $"https://docs.google.com/forms/d/{formId}/edit";
        return (formId, responderUri, editUrl);
    }

    private static object TextQuestion(string title, int index, bool required, bool paragraph = false) =>
        new
        {
            createItem = new
            {
                item = new
                {
                    title,
                    questionItem = new
                    {
                        question = new
                        {
                            required,
                            textQuestion = new { paragraph }
                        }
                    }
                },
                location = new { index }
            }
        };

    private static object ChoiceQuestion(string title, int index, string[] options) =>
        new
        {
            createItem = new
            {
                item = new
                {
                    title,
                    questionItem = new
                    {
                        question = new
                        {
                            required = true,
                            choiceQuestion = new
                            {
                                type = "RADIO",
                                options = options.Select(v => new { value = v }).ToArray()
                            }
                        }
                    }
                },
                location = new { index }
            }
        };

    private async Task TryShareFormAsync(HttpClient client, string formId)
    {
        try
        {
            using var anyone = await client.PostAsync(
                $"https://www.googleapis.com/drive/v3/files/{formId}/permissions?supportsAllDrives=true",
                ToJsonContent(new { role = "reader", type = "anyone" }));
            if (!anyone.IsSuccessStatusCode)
                _logger.LogWarning("Không set quyền anyone cho Form {FormId}: {Body}", formId, await anyone.Content.ReadAsStringAsync());

            var shareEmail = _configuration["GoogleIntegration:ShareWithEmail"];
            if (!string.IsNullOrWhiteSpace(shareEmail))
            {
                using var writer = await client.PostAsync(
                    $"https://www.googleapis.com/drive/v3/files/{formId}/permissions?sendNotificationEmail=false",
                    ToJsonContent(new { role = "writer", type = "user", emailAddress = shareEmail.Trim() }));
                if (!writer.IsSuccessStatusCode)
                    _logger.LogWarning("Không share Form {FormId} cho {Email}: {Body}", formId, shareEmail, await writer.Content.ReadAsStringAsync());
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Share Google Form {FormId} thất bại", formId);
        }
    }

    private async Task<string?> GetGoogleAccessTokenAsync()
    {
        var scopes = new[]
        {
            "https://www.googleapis.com/auth/forms.body",
            "https://www.googleapis.com/auth/drive.file",
            "https://www.googleapis.com/auth/drive"
        };

        var jsonPath = _configuration["GoogleIntegration:ServiceAccountJsonPath"];
        if (!string.IsNullOrWhiteSpace(jsonPath))
        {
            var fullPath = Path.IsPathRooted(jsonPath)
                ? jsonPath
                : Path.Combine(_env.ContentRootPath, jsonPath);
            if (System.IO.File.Exists(fullPath))
            {
                var credential = GoogleCredential.FromFile(fullPath).CreateScoped(scopes);
                return await credential.UnderlyingCredential.GetAccessTokenForRequestAsync();
            }
        }

        var jsonInline = _configuration["GoogleIntegration:ServiceAccountJson"];
        if (!string.IsNullOrWhiteSpace(jsonInline) && jsonInline.Contains("client_email", StringComparison.OrdinalIgnoreCase))
        {
            var credential = GoogleCredential.FromJson(jsonInline).CreateScoped(scopes);
            return await credential.UnderlyingCredential.GetAccessTokenForRequestAsync();
        }

        var clientId = _configuration["GoogleIntegration:ClientId"] ?? _configuration["GoogleAuth:ClientId"];
        var clientSecret = _configuration["GoogleIntegration:ClientSecret"];
        var refreshToken = _configuration["GoogleIntegration:RefreshToken"];
        if (!string.IsNullOrWhiteSpace(clientId) && !string.IsNullOrWhiteSpace(clientSecret) && !string.IsNullOrWhiteSpace(refreshToken))
        {
            var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
            {
                ClientSecrets = new ClientSecrets { ClientId = clientId, ClientSecret = clientSecret },
                Scopes = scopes
            });
            var token = new TokenResponse { RefreshToken = refreshToken };
            var userCred = new UserCredential(flow, "hr-campaign", token);
            return await userCred.GetAccessTokenForRequestAsync();
        }

        return null;
    }

    private static StringContent ToJsonContent(object payload) =>
        new(JsonSerializer.Serialize(payload, JsonOptions), Encoding.UTF8, "application/json");

    public string BuildWebhookAppsScript(Guid jobId, string? customApiBase = null)
    {
        var (apiBase, _, secret) = GetPublicConfig();
        if (!string.IsNullOrWhiteSpace(customApiBase))
        {
            apiBase = customApiBase.Trim().TrimEnd('/');
        }
        return BuildAppsScript(jobId, apiBase, secret);
    }

    private static string BuildAppsScript(Guid jobId, string apiBase, string secret)
    {
        return $$"""
function onFormSubmit(e) {
  // Hỗ trợ cả Trigger gắn trên Google Sheet (e.namedValues) và Trigger gắn trực tiếp trên Google Form (e.response)
  const rawMap = {};
  if (e && e.namedValues) {
    Object.keys(e.namedValues).forEach(k => {
      rawMap[k] = (e.namedValues[k] && e.namedValues[k][0]) ? String(e.namedValues[k][0]).trim() : '';
    });
  } else if (e && e.response && typeof e.response.getItemResponses === 'function') {
    e.response.getItemResponses().forEach(ir => {
      rawMap[ir.getItem().getTitle()] = String(ir.getResponse() || '').trim();
    });
  }

  const normalize = (s) => String(s || '').normalize('NFD').replace(/[\u0300-\u036f]/g, '').replace(/[đĐ]/g, 'd').toLowerCase().trim();
  const usedKeys = {};

  const findByKeywords = (keywords) => {
    for (const key of Object.keys(rawMap)) {
      const nk = normalize(key);
      if (keywords.some(kw => nk.indexOf(kw) !== -1)) {
        usedKeys[key] = true;
        return rawMap[key];
      }
    }
    return '';
  };

  const fullName = findByKeywords(['ho va ten', 'ho ten', 'ten cua ban', 'ten ung vien', 'full name', 'fullname', 'name', 'ten']);
  const phone = findByKeywords(['so dien thoai', 'dien thoai', 'sdt', 'phone', 'mobile', 'di dong', 'zalo']);
  const email = findByKeywords(['email', 'gmail', 'thu dien tu', 'mail']);
  const yearOfBirth = findByKeywords(['nam sinh', 'ngay sinh', 'birth', 'dob', 'tuoi']);
  const hometown = findByKeywords(['que quan', 'thuong tru', 'dia chi', 'noi o', 'cho o', 'hometown', 'address']);
  const shiftReady = findByKeywords(['3 ca', 'xoay ca', 'tang ca', 'ca kip', 'shift']);
  let experienceNotes = findByKeywords(['kinh nghiem', 'experience', 'nam lam viec', 'ky nang']);

  // Gom tự động tất cả các câu hỏi tùy chỉnh khác trong Form để không mất dữ liệu
  const extraParts = [];
  if (experienceNotes) extraParts.push(experienceNotes);
  Object.keys(rawMap).forEach(k => {
    if (usedKeys[k]) return;
    const nk = normalize(k);
    if (nk.indexOf('dau thoi gian') !== -1 || nk.indexOf('timestamp') !== -1) return;
    if (rawMap[k]) extraParts.push(k + ': ' + rawMap[k]);
  });

  const payload = {
    jobId: '{{jobId}}',
    fullName: fullName,
    phone: phone,
    yearOfBirth: yearOfBirth,
    hometown: hometown,
    experienceNotes: extraParts.join(' | '),
    shiftReady: shiftReady,
    email: email,
    customFields: rawMap
  };

  const webhookUrl = '{{apiBase}}/api/applications/webhook-google-form';

  const options = {
    method: 'post',
    contentType: 'application/json',
    headers: {
      'X-Webhook-Secret': '{{secret}}',
      'ngrok-skip-browser-warning': 'true'
    },
    payload: JSON.stringify(payload),
    muteHttpExceptions: true
  };

  try {
    const response = UrlFetchApp.fetch(webhookUrl, options);
    Logger.log('ATS Webhook status: ' + response.getResponseCode());
    Logger.log('ATS Webhook response: ' + response.getContentText());
  } catch (err) {
    Logger.log('Lỗi gọi ATS Webhook: ' + err.toString());
  }
}
""";
    }
}
