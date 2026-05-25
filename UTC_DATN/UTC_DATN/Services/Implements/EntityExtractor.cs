using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using UTC_DATN.DTOs.Common;

namespace UTC_DATN.Services.Implements;

/// <summary>
/// Dịch vụ trích xuất entity (ROLE, SALARY, LOCATION, SKILL, etc.) từ tin nhắn
/// </summary>
public class EntityExtractor
{
    private readonly ILogger<EntityExtractor> _logger;

    public EntityExtractor(ILogger<EntityExtractor> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Trích xuất tất cả entity từ tin nhắn
    /// </summary>
    public async Task<List<ExtractedEntityDto>> ExtractAsync(string message)
    {
        var entities = new List<ExtractedEntityDto>();

        try
        {
            // Trích xuất ROLE
            var roleEntity = ExtractRole(message);
            if (roleEntity != null)
                entities.Add(roleEntity);

            // Trích xuất EXPERIENCE_LEVEL
            var levelEntity = ExtractExperienceLevel(message);
            if (levelEntity != null)
                entities.Add(levelEntity);

            // Trích xuất SALARY_RANGE
            var salaryEntity = ExtractSalaryRange(message);
            if (salaryEntity != null)
                entities.Add(salaryEntity);

            // Trích xuất LOCATION
            var locationEntity = ExtractLocation(message);
            if (locationEntity != null)
                entities.Add(locationEntity);

            // Trích xuất EMPLOYMENT_TYPE
            var employmentEntity = ExtractEmploymentType(message);
            if (employmentEntity != null)
                entities.Add(employmentEntity);

            // Trích xuất DURATION
            var durationEntity = ExtractDuration(message);
            if (durationEntity != null)
                entities.Add(durationEntity);

            // Trích xuất SENIORITY
            var seniorityEntity = ExtractSeniority(message);
            if (seniorityEntity != null)
                entities.Add(seniorityEntity);

            // Trích xuất SKILLS (có thể nhiều)
            var skillEntities = ExtractSkills(message);
            entities.AddRange(skillEntities);

            _logger.LogDebug($"[Entity Extraction] Extracted {entities.Count} entities from message");
            return entities;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during entity extraction");
            return entities;
        }
    }

    /// <summary>
    /// Trích xuất ROLE (Backend, Frontend, DevOps, etc.)
    /// </summary>
    private ExtractedEntityDto? ExtractRole(string message)
    {
        var roles = new string[] 
        { 
            "backend", "frontend", "fullstack", "devops", "qa", "test", "sre",
            "data engineer", "ml engineer", "mobile", "android", "ios",
            "ba", "pm", "designer", "architect", "lead"
        };

        var lower = message.ToLower();
        foreach (var role in roles)
        {
            if (Regex.IsMatch(lower, $@"\b{role}\b", RegexOptions.IgnoreCase))
            {
                return new ExtractedEntityDto
                {
                    Type = "ROLE",
                    Value = role,
                    Confidence = 0.95
                };
            }
        }

        return null;
    }

    /// <summary>
    /// Trích xuất EXPERIENCE_LEVEL (Junior, Middle, Senior)
    /// </summary>
    private ExtractedEntityDto? ExtractExperienceLevel(string message)
    {
        var lower = message.ToLower();

        if (Regex.IsMatch(lower, @"\b(junior|intern|fresher|0-2|mới|tập sự)\b"))
            return new ExtractedEntityDto { Type = "EXPERIENCE_LEVEL", Value = "Junior", Confidence = 0.9 };

        if (Regex.IsMatch(lower, @"\b(middle|mid|2-5|trung cấp|trung bình)\b"))
            return new ExtractedEntityDto { Type = "EXPERIENCE_LEVEL", Value = "Middle", Confidence = 0.9 };

        if (Regex.IsMatch(lower, @"\b(senior|5\+|cao cấp|lâu năm|lead)\b"))
            return new ExtractedEntityDto { Type = "EXPERIENCE_LEVEL", Value = "Senior", Confidence = 0.9 };

        return null;
    }

    /// <summary>
    /// Trích xuất SALARY_RANGE
    /// </summary>
    private ExtractedEntityDto? ExtractSalaryRange(string message)
    {
        var lower = message.ToLower();

        // Tìm pattern: "1000-2000", "1,000-2,000", "$1000", etc.
        var match = Regex.Match(lower, @"(\d{1,2},?\d{3}|\d{1,4})\s*[-–]\s*(\d{1,2},?\d{3}|\d{1,4})");
        if (match.Success)
        {
            var min = match.Groups[1].Value.Replace(",", "");
            var max = match.Groups[2].Value.Replace(",", "");
            return new ExtractedEntityDto
            {
                Type = "SALARY_RANGE",
                Value = $"{min}-{max}",
                Confidence = 0.9
            };
        }

        // Tìm "từ X", "tối thiểu X", "từ X trở lên"
        match = Regex.Match(lower, @"(?:từ|tối thiểu|từ)\s*(\d{1,2},?\d{3}|\d{1,4})");
        if (match.Success)
        {
            var salary = match.Groups[1].Value.Replace(",", "");
            return new ExtractedEntityDto
            {
                Type = "SALARY_RANGE",
                Value = $"{salary}+",
                Confidence = 0.8
            };
        }

        return null;
    }

    /// <summary>
    /// Trích xuất LOCATION
    /// </summary>
    private ExtractedEntityDto? ExtractLocation(string message)
    {
        var locations = new string[] 
        { 
            "hà nội", "ho chi minh", "hcm", "tp hcm", "sài gòn", "đà nẵng", 
            "hải phòng", "cần thơ", "quảng ninh", "hải dương", "hưng yên",
            "ba rịa", "vũng tàu", "bình dương", "đồng nai", "long an"
        };

        var lower = message.ToLower();
        foreach (var loc in locations)
        {
            if (Regex.IsMatch(lower, $@"\b{loc}\b", RegexOptions.IgnoreCase))
            {
                return new ExtractedEntityDto
                {
                    Type = "LOCATION",
                    Value = loc,
                    Confidence = 0.95
                };
            }
        }

        return null;
    }

    /// <summary>
    /// Trích xuất EMPLOYMENT_TYPE
    /// </summary>
    private ExtractedEntityDto? ExtractEmploymentType(string message)
    {
        var lower = message.ToLower();

        if (Regex.IsMatch(lower, @"\b(toàn thời gian|full-time|fulltime)\b"))
            return new ExtractedEntityDto { Type = "EMPLOYMENT_TYPE", Value = "Full-time", Confidence = 0.95 };

        if (Regex.IsMatch(lower, @"\b(bán thời gian|part-time|parttime)\b"))
            return new ExtractedEntityDto { Type = "EMPLOYMENT_TYPE", Value = "Part-time", Confidence = 0.95 };

        if (Regex.IsMatch(lower, @"\b(freelance|tự do|dạo)\b"))
            return new ExtractedEntityDto { Type = "EMPLOYMENT_TYPE", Value = "Freelance", Confidence = 0.95 };

        if (Regex.IsMatch(lower, @"\b(thực tập|intern|internship)\b"))
            return new ExtractedEntityDto { Type = "EMPLOYMENT_TYPE", Value = "Internship", Confidence = 0.95 };

        return null;
    }

    /// <summary>
    /// Trích xuất DURATION
    /// </summary>
    private ExtractedEntityDto? ExtractDuration(string message)
    {
        var lower = message.ToLower();

        // Pattern: "3 năm", "5+ năm", "10 years", etc.
        var match = Regex.Match(lower, @"(\d+)\s*(?:\+)?\s*(?:năm|year|năm kinh nghiệm)");
        if (match.Success)
        {
            var years = match.Groups[1].Value;
            var isPlus = message.Contains("+") ? "+" : "";
            return new ExtractedEntityDto
            {
                Type = "DURATION",
                Value = $"{years}{isPlus} năm",
                Confidence = 0.9
            };
        }

        return null;
    }

    /// <summary>
    /// Trích xuất SENIORITY
    /// </summary>
    private ExtractedEntityDto? ExtractSeniority(string message)
    {
        var lower = message.ToLower();

        if (Regex.IsMatch(lower, @"\b(intern|tập sự)\b"))
            return new ExtractedEntityDto { Type = "SENIORITY", Value = "Intern", Confidence = 0.95 };

        if (Regex.IsMatch(lower, @"\b(junior|fresher)\b"))
            return new ExtractedEntityDto { Type = "SENIORITY", Value = "Junior", Confidence = 0.95 };

        if (Regex.IsMatch(lower, @"\b(middle|mid-level)\b"))
            return new ExtractedEntityDto { Type = "SENIORITY", Value = "Middle", Confidence = 0.95 };

        if (Regex.IsMatch(lower, @"\b(senior)\b"))
            return new ExtractedEntityDto { Type = "SENIORITY", Value = "Senior", Confidence = 0.95 };

        if (Regex.IsMatch(lower, @"\b(lead|tech lead|lead engineer)\b"))
            return new ExtractedEntityDto { Type = "SENIORITY", Value = "Lead", Confidence = 0.95 };

        return null;
    }

    /// <summary>
    /// Trích xuất SKILLS (có thể nhiều)
    /// </summary>
    private List<ExtractedEntityDto> ExtractSkills(string message)
    {
        var skills = new string[] 
        { 
            // Backend
            "java", "python", "go", "rust", "c#", "c++", "php", "ruby",
            "spring boot", "django", "fastapi", "node.js", "express",
            
            // Frontend
            "react", "vue", "angular", "typescript", "javascript", "html", "css",
            "tailwind", "bootstrap", "webpack",
            
            // Database
            "sql", "postgres", "mysql", "mongodb", "redis", "elasticsearch",
            "cassandra", "dynamodb",
            
            // DevOps/Infra
            "docker", "kubernetes", "aws", "gcp", "azure", "terraform",
            "jenkins", "github actions", "git",
            
            // Concepts
            "system design", "microservices", "api", "rest", "graphql",
            "testing", "agile", "scrum", "jira", "git"
        };

        var extractedSkills = new List<ExtractedEntityDto>();
        var lower = message.ToLower();

        foreach (var skill in skills)
        {
            if (Regex.IsMatch(lower, $@"\b{Regex.Escape(skill)}\b", RegexOptions.IgnoreCase))
            {
                extractedSkills.Add(new ExtractedEntityDto
                {
                    Type = "SKILL",
                    Value = skill,
                    Confidence = 0.9
                });
            }
        }

        return extractedSkills;
    }
}
