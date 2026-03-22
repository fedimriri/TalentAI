using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using TalentAI.Configurations;
using TalentAI.Data;
using TalentAI.Models;

namespace TalentAI.Services;

public class MatchingService : IMatchingService
{
    private readonly MongoDbContext _context;
    private readonly ILogger<MatchingService> _logger;
    private readonly HttpClient _httpClient;
    private readonly string _groqApiKey;

    // Default ATS weights (Skills=50%, Experience=30%, Education=20%)
    private const double DefaultSkillWeight = 0.5;
    private const double DefaultExperienceWeight = 0.3;
    private const double DefaultEducationWeight = 0.2;

    private const string GroqEndpoint = "https://api.groq.com/openai/v1/chat/completions";
    private const string GroqModel = "llama-3.1-8b-instant";

    public MatchingService(
        MongoDbContext context, 
        ILogger<MatchingService> logger, 
        HttpClient httpClient, 
        IOptions<AISettings> aiSettings)
    {
        _context = context;
        _logger = logger;
        _httpClient = httpClient;
        _groqApiKey = aiSettings.Value.GroqApiKey;
    }

    public async Task<MatchingResult> CalculateMatchAsync(string jobId, string applicationId)
    {
        // Prevent duplicate
        var existing = await _context.MatchingResults
            .Find(m => m.JobApplicationId == applicationId)
            .FirstOrDefaultAsync();

        if (existing != null)
        {
            _logger.LogInformation("MatchingResult already exists for application {ApplicationId}, skipping.", applicationId);
            return existing;
        }

        // Fetch parsed data
        var parsedResume = await _context.ParsedResumes
            .Find(p => p.JobApplicationId == applicationId)
            .FirstOrDefaultAsync();

        var parsedJob = await _context.ParsedJobs
            .Find(p => p.JobId == jobId)
            .FirstOrDefaultAsync();

        var job = await _context.Jobs
            .Find(j => j.Id == jobId)
            .FirstOrDefaultAsync();

        // Build result with weights
        var result = new MatchingResult
        {
            CandidateId = parsedResume?.CandidateId ?? string.Empty,
            JobId = jobId,
            JobApplicationId = applicationId,
            SkillWeight = DefaultSkillWeight,
            ExperienceWeight = DefaultExperienceWeight,
            EducationWeight = DefaultEducationWeight,
            CreatedAt = DateTime.UtcNow
        };

        // Missing data → save empty result
        if (parsedResume == null || parsedJob == null)
        {
            _logger.LogWarning(
                "Missing parsed data — Resume:{HasResume}, Job:{HasJob} (App:{AppId})",
                parsedResume != null, parsedJob != null, applicationId);

            await _context.MatchingResults.InsertOneAsync(result);
            return result;
        }

        try
        {
            // Compute individual scores
            ComputeSkillScore(result, parsedResume, parsedJob);
            ComputeExperienceScore(result, parsedResume, parsedJob);
            ComputeEducationScore(result, parsedResume);
            ComputeTotalScore(result); // This is the Base / Keyword score

            double baseScore = result.TotalScore;

            // Phase 3.6: AI Semantic Matching via Groq
            if (!string.IsNullOrEmpty(_groqApiKey) && !string.IsNullOrWhiteSpace(parsedResume.RawText))
            {
                var aiScoreResult = await CallGroqApiAsync(parsedResume.RawText, job.Description);
                if (aiScoreResult != null)
                {
                    result.AiScore = aiScoreResult.Value.Score;
                    result.AiAnalysis = aiScoreResult.Value.Analysis;
                    result.AiEnhanced = true;

                    // Blend scores: 60% Keyword / 40% AI
                    result.TotalScore = Math.Round((baseScore * 0.6) + (result.AiScore * 0.4), 1);
                    
                    _logger.LogInformation(
                        "[AI MATCHING] App:{AppId} BaseScore:{Base} AiScore:{Ai} FinalScore:{Final}",
                        applicationId, baseScore, result.AiScore, result.TotalScore);
                }
            }

            // Build ScoreBreakdown JSON including AI results
            result.ScoreBreakdown = JsonSerializer.Serialize(new
            {
                baseScore = baseScore,
                skills = result.SkillScore,
                experience = result.ExperienceScore,
                education = result.EducationScore,
                total = result.TotalScore,
                aiEnhanced = result.AiEnhanced,
                aiScore = result.AiScore,
                weights = new
                {
                    skills = result.SkillWeight,
                    experience = result.ExperienceWeight,
                    education = result.EducationWeight
                },
                matchedSkills = result.MatchedSkills,
                missingSkills = result.MissingSkills
            });

            _logger.LogInformation(
                "[ATS SCORE] App:{AppId} Skills:{SkillScore} Experience:{ExpScore} Education:{EduScore} Total:{TotalScore}",
                applicationId, result.SkillScore, result.ExperienceScore,
                result.EducationScore, result.TotalScore);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to calculate ATS score for application {ApplicationId}", applicationId);
        }

        await _context.MatchingResults.InsertOneAsync(result);
        return result;
    }

    /// <summary>
    /// SkillScore = (MatchedSkills / RequiredSkills) * 100
    /// Fallback: 0 if no required skills.
    /// </summary>
    private static void ComputeSkillScore(MatchingResult result, ParsedResume resume, ParsedJob job)
    {
        var resumeSkills = resume.Skills
            .Select(s => s.Trim().ToLower())
            .ToHashSet();

        var jobSkills = job.RequiredSkills
            .Select(s => s.Trim().ToLower())
            .Distinct()
            .ToList();

        var matched = jobSkills.Where(s => resumeSkills.Contains(s)).ToList();
        var missing = jobSkills.Where(s => !resumeSkills.Contains(s)).ToList();

        result.MatchedSkills = matched.Select(GetDisplayName).ToList();
        result.MissingSkills = missing.Select(GetDisplayName).ToList();

        result.SkillScore = jobSkills.Count > 0
            ? Math.Round((double)matched.Count / jobSkills.Count * 100, 1)
            : 0;
    }

    /// <summary>
    /// ExperienceScore: Math.Min(candidate/required, 1.0) * 100
    /// If requiredYears == 0 → score = 100.
    /// </summary>
    private void ComputeExperienceScore(MatchingResult result, ParsedResume resume, ParsedJob job)
    {
        double candidateExp = resume.ExperienceYears;
        double requiredExp = job.RequiredExperienceYears;

        if (requiredExp <= 0)
        {
            result.ExperienceScore = 100;
        }
        else
        {
            result.ExperienceScore = Math.Round(Math.Min(candidateExp / requiredExp, 1.0) * 100, 1);
        }

        _logger.LogInformation(
            "[EXP SCORE] CandidateExp:{Candidate} RequiredExp:{Required} Score:{Score}",
            candidateExp, requiredExp, result.ExperienceScore);
    }

    /// <summary>
    /// EducationScore based on keyword hierarchy:
    /// PhD/Doctorate=100, Master/Engineering=80, Bachelor/Licence=60, Other=40.
    /// </summary>
    private static void ComputeEducationScore(MatchingResult result, ParsedResume resume)
    {
        var edu = (resume.Education ?? string.Empty).ToLower();

        if (string.IsNullOrWhiteSpace(edu))
        {
            result.EducationScore = 40;
            return;
        }

        if (edu.Contains("phd") || edu.Contains("doctorat") || edu.Contains("doctorate"))
        {
            result.EducationScore = 100;
        }
        else if (edu.Contains("master") || edu.Contains("engineering") ||
                 edu.Contains("ingénieur") || edu.Contains("ingenieur") ||
                 edu.Contains("mba") || edu.Contains("msc") || edu.Contains("m.sc"))
        {
            result.EducationScore = 80;
        }
        else if (edu.Contains("bachelor") || edu.Contains("licence") ||
                 edu.Contains("bsc") || edu.Contains("b.sc") || edu.Contains("degree"))
        {
            result.EducationScore = 60;
        }
        else
        {
            result.EducationScore = 40;
        }
    }

    /// <summary>
    /// TotalScore = weighted sum of individual scores.
    /// </summary>
    private static void ComputeTotalScore(MatchingResult result)
    {
        result.TotalScore = Math.Round(
            (result.SkillScore * result.SkillWeight) +
            (result.ExperienceScore * result.ExperienceWeight) +
            (result.EducationScore * result.EducationWeight),
            1);
    }

    private static string GetDisplayName(string lowercaseSkill)
    {
        for (int i = 0; i < ParsingKeywords.SkillKeywords.Length; i++)
        {
            if (ParsingKeywords.SkillKeywords[i] == lowercaseSkill)
                return ParsingKeywords.SkillDisplayNames[i];
        }
        return lowercaseSkill;
    }

    private async Task<(double Score, string Analysis)?> CallGroqApiAsync(string resumeText, string jobDescription)
    {
        try
        {
            var prompt = $@"
Analyze the candidate's resume against the job description.
Provide a semantic match score from 0 to 100 based on overall fit, not just keywords.
Also provide a very short 1-2 sentence explanation.

Resume:
{resumeText}

Job Description:
{jobDescription}

Respond strictly in this JSON format:
{{
    ""score"": 85,
    ""analysis"": ""Candidate has strong background in requested frontend frameworks.""
}}";

            var requestBody = new
            {
                model = GroqModel,
                messages = new[]
                {
                    new { role = "user", content = prompt }
                },
                response_format = new { type = "json_object" },
                temperature = 0.2
            };

            var request = new HttpRequestMessage(HttpMethod.Post, GroqEndpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _groqApiKey);
            request.Content = new StringContent(JsonSerializer.Serialize(requestBody), System.Text.Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("[AI MATCHING] Groq API returned {StatusCode}", response.StatusCode);
                return null;
            }

            var jsonResponse = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(jsonResponse);
            
            var contentString = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            if (string.IsNullOrEmpty(contentString)) return null;

            using var resultDoc = JsonDocument.Parse(contentString);
            var root = resultDoc.RootElement;

            double score = 0;
            if (root.TryGetProperty("score", out var scoreElement))
            {
                if (scoreElement.ValueKind == JsonValueKind.Number) score = scoreElement.GetDouble();
                else if (scoreElement.ValueKind == JsonValueKind.String && double.TryParse(scoreElement.GetString(), out var s)) score = s;
            }

            string analysis = root.TryGetProperty("analysis", out var analysisElement) 
                ? analysisElement.GetString() ?? "" 
                : "";

            return (Math.Clamp(score, 0, 100), analysis);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AI MATCHING] Failed to call Groq API.");
            return null;
        }
    }
}
