using System.Text.Json;
using MongoDB.Driver;
using TalentAI.Data;
using TalentAI.Models;

namespace TalentAI.Services;

public class MatchingService : IMatchingService
{
    private readonly MongoDbContext _context;
    private readonly ILogger<MatchingService> _logger;

    // Default ATS weights (Skills=50%, Experience=30%, Education=20%)
    private const double DefaultSkillWeight = 0.5;
    private const double DefaultExperienceWeight = 0.3;
    private const double DefaultEducationWeight = 0.2;

    public MatchingService(MongoDbContext context, ILogger<MatchingService> logger)
    {
        _context = context;
        _logger = logger;
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
            ComputeTotalScore(result);

            // Build ScoreBreakdown JSON
            result.ScoreBreakdown = JsonSerializer.Serialize(new
            {
                skills = result.SkillScore,
                experience = result.ExperienceScore,
                education = result.EducationScore,
                total = result.TotalScore,
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
    /// ExperienceScore: 100 if candidate >= required, else proportional.
    /// Uses month-level precision (double).
    /// </summary>
    private static void ComputeExperienceScore(MatchingResult result, ParsedResume resume, ParsedJob job)
    {
        var candidateYears = resume.ExperienceYears;
        var requiredYears = job.RequiredExperienceYears;

        if (requiredYears <= 0)
        {
            result.ExperienceScore = 100;
        }
        else if (candidateYears >= requiredYears)
        {
            result.ExperienceScore = 100;
        }
        else
        {
            result.ExperienceScore = Math.Round(candidateYears / requiredYears * 100, 1);
        }
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
}
