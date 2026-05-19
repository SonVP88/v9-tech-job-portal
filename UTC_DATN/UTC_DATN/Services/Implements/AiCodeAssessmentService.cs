using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using UTC_DATN.Data;
using UTC_DATN.DTOs.AiCodeAssessment;
using UTC_DATN.Entities;
using UTC_DATN.Services.Interfaces;

namespace UTC_DATN.Services.Implements
{
    public class AiCodeAssessmentService : IAiCodeAssessmentService
    {
        private readonly UTC_DATNContext _context;
        private readonly HttpClient _httpClient;
        private readonly ILogger<AiCodeAssessmentService> _logger;
        private readonly string _groqApiKey;

        public AiCodeAssessmentService(
            UTC_DATNContext context, 
            IHttpClientFactory httpClientFactory,
            ILogger<AiCodeAssessmentService> logger,
            IConfiguration config)
        {
            _context = context;
            _httpClient = httpClientFactory.CreateClient();
            _logger = logger;
            _groqApiKey = config["GroqAI:ApiKey"] ?? string.Empty;
        }

        public async Task<CodeChallengeDto> CreateChallengeAsync(CreateCodeChallengeDto request)
        {
            var challenge = new CodeChallenge
            {
                JobId = request.JobId,
                Title = request.Title,
                ProblemStatement = request.ProblemStatement,
                InitialCodeText = request.InitialCodeText,
                Language = request.Language,
                Difficulty = request.Difficulty,
                MaxTimeMinutes = request.MaxTimeMinutes,
                ExpectedOutput = request.ExpectedOutput
            };

            _context.Set<CodeChallenge>().Add(challenge);
            await _context.SaveChangesAsync();

            return MapToDto(challenge);
        }

        public async Task<List<CodeChallengeDto>> GetChallengesByJobAsync(Guid jobId)
        {
            var list = await _context.Set<CodeChallenge>()
                .Where(x => x.JobId == jobId && x.IsActive)
                .ToListAsync();

            return list.Select(MapToDto).ToList();
        }

        public async Task<CodeChallengeDto?> GetChallengeByIdAsync(Guid challengeId)
        {
            var c = await _context.Set<CodeChallenge>().FindAsync(challengeId);
            if (c == null) return null;
            return MapToDto(c);
        }

        public async Task<CodeSubmissionDto> SubmitCodeAsync(SubmitCodeDto request)
        {
            var submission = new CandidateCodeSubmission
            {
                ChallengeId = request.ChallengeId,
                CandidateId = request.CandidateId,
                ApplicationId = request.ApplicationId,
                SubmittedCode = request.SubmittedCode,
                Language = request.Language,
                StartTime = request.StartTime,
                Status = "PENDING_REVIEW"
            };

            _context.Set<CandidateCodeSubmission>().Add(submission);
            await _context.SaveChangesAsync();

            // Fire and forget AI analysis background task (in real app, use Queue/Hangfire)
            _ = Task.Run(() => AnalyzeCodeWithAiAsync(submission.SubmissionId));

            return new CodeSubmissionDto
            {
                SubmissionId = submission.SubmissionId,
                ChallengeId = submission.ChallengeId,
                CandidateId = submission.CandidateId,
                ApplicationId = submission.ApplicationId,
                SubmittedCode = submission.SubmittedCode,
                Status = submission.Status,
                SubmittedAt = submission.SubmittedAt
            };
        }

        public async Task<AiCodeReviewResponseDto> GetOrCreateReviewAsync(Guid submissionId)
        {
            var existingReview = await _context.Set<AiCodeReviewReport>()
                .FirstOrDefaultAsync(r => r.SubmissionId == submissionId);

            if (existingReview != null)
            {
                return MapToReviewDto(existingReview);
            }

            // Fallback: If not processed yet, process it now
            return await AnalyzeCodeWithAiAsync(submissionId);
        }

        public async Task<AiCodeReviewResponseDto> AnalyzeCodeWithAiAsync(Guid submissionId)
        {
            var submission = await _context.Set<CandidateCodeSubmission>()
                .Include(s => s.Challenge)
                .FirstOrDefaultAsync(s => s.SubmissionId == submissionId);

            if (submission == null || submission.Challenge == null)
                throw new Exception("Submission or Challenge not found");

            // Build Prompt for OpenAI
            var prompt = $@"
You are an expert Senior SWE and Technical Interviewer. 
Review the following {submission.Language} code submitted by a candidate.

Problem Statement:
{submission.Challenge.ProblemStatement}

Candidate's Code:
```
{submission.SubmittedCode}
```

Provide your feedback in strict JSON format matching exactly this structure:
{{
  ""totalScore"": 85, // 0 to 100
  ""timeComplexity"": ""O(N)"",
  ""spaceComplexity"": ""O(1)"",
  ""codeSmells"": [""Magic numbers used"", ""Variable names unclear""],
  ""securityVulnerabilities"": [""SQL Injection vulnerability""],
  ""overallFeedback"": ""Good approach but lacking edge case handling."",
  ""suggestions"": [""Extract method to reduce complexity"", ""Add null check""]
}}
Return ONLY valid JSON.
";

            // Call Groq API (Llama 3 70b)
            JsonDocument? aiResultData;
            
            if (string.IsNullOrEmpty(_groqApiKey))
            {
                // Mock Response for Demo/Local if no real API key
                var mockJson = $@"{{
                  ""totalScore"": 80,
                  ""timeComplexity"": ""O(N)"",
                  ""spaceComplexity"": ""O(N)"",
                  ""codeSmells"": [""Missing standard indentation"", ""Unnecessary iterations""],
                  ""securityVulnerabilities"": [],
                  ""overallFeedback"": ""The code correctly solves the problem for basic cases but has poor variable naming and lacks comments. Overall good effort."",
                  ""suggestions"": [""Use meaningful variable names"", ""Consider using a Hash Map for faster lookups O(1)""]
                }}";
                aiResultData = JsonDocument.Parse(mockJson);
                await Task.Delay(2000); // Simulate API latency
            }
            else
            {
                // Real Groq request
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _groqApiKey);
                var requestBody = new
                {
                    model = "llama3-70b-8192", // Top tier model on Groq
                    messages = new[] {
                        new { role = "system", content = "You are a senior tech interviewer AI. You must ONLY output valid JSON." },
                        new { role = "user", content = prompt }
                    },
                    temperature = 0.2,
                    response_format = new { type = "json_object" }
                };

                var response = await _httpClient.PostAsJsonAsync("https://api.groq.com/openai/v1/chat/completions", requestBody);
                
                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"Groq API Error: {error}");
                    throw new Exception("AI Provider Error");
                }

                var responseData = await response.Content.ReadFromJsonAsync<JsonElement>();
                var contentString = responseData.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
                aiResultData = JsonDocument.Parse(contentString!);
            }

            var root = aiResultData.RootElement;
            var report = new AiCodeReviewReport
            {
                SubmissionId = submissionId,
                TotalScore = root.GetProperty("totalScore").GetInt32(),
                TimeComplexity = root.GetProperty("timeComplexity").GetString(),
                SpaceComplexity = root.GetProperty("spaceComplexity").GetString(),
                CodeSmells = root.GetProperty("codeSmells").ToString(), // Serialize Array 
                SecurityVulnerabilities = root.GetProperty("securityVulnerabilities").ToString(),
                OverallFeedback = root.GetProperty("overallFeedback").GetString() ?? "",
                Suggestions = root.GetProperty("suggestions").ToString(),
                AiProvider = "GROQ_LLAMA3_70B"
            };

            _context.Set<AiCodeReviewReport>().Add(report);
            
            // Mark submission as reviewed
            submission.Status = "REVIEWED";

            await _context.SaveChangesAsync();
            return MapToReviewDto(report);
        }

        private CodeChallengeDto MapToDto(CodeChallenge challenge)
        {
            return new CodeChallengeDto
            {
                ChallengeId = challenge.ChallengeId,
                JobId = challenge.JobId,
                Title = challenge.Title,
                ProblemStatement = challenge.ProblemStatement,
                InitialCodeText = challenge.InitialCodeText,
                Language = challenge.Language,
                Difficulty = challenge.Difficulty,
                MaxTimeMinutes = challenge.MaxTimeMinutes
            };
        }

        private AiCodeReviewResponseDto MapToReviewDto(AiCodeReviewReport report)
        {
            return new AiCodeReviewResponseDto
            {
                ReviewId = report.ReviewId,
                SubmissionId = report.SubmissionId,
                TotalScore = report.TotalScore,
                TimeComplexity = report.TimeComplexity,
                SpaceComplexity = report.SpaceComplexity,
                CodeSmells = string.IsNullOrEmpty(report.CodeSmells) ? new List<string>() : JsonSerializer.Deserialize<List<string>>(report.CodeSmells) ?? new List<string>(),
                SecurityVulnerabilities = string.IsNullOrEmpty(report.SecurityVulnerabilities) ? new List<string>() : JsonSerializer.Deserialize<List<string>>(report.SecurityVulnerabilities) ?? new List<string>(),
                OverallFeedback = report.OverallFeedback,
                Suggestions = string.IsNullOrEmpty(report.Suggestions) ? new List<string>() : JsonSerializer.Deserialize<List<string>>(report.Suggestions) ?? new List<string>(),
                ReviewedAt = report.ReviewedAt,
                AiProvider = report.AiProvider
            };
        }
    }
}
