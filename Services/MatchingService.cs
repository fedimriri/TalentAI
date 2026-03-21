using MongoDB.Driver;
using TalentAI.Data;
using TalentAI.Models;

namespace TalentAI.Services;

public class MatchingService : IMatchingService
{
    private readonly MongoDbContext _context;
    private readonly ILogger<MatchingService> _logger;

    // Scoring weights
    private const double SkillWeight = 0.7;
    private const double ExperienceWeight = 0.3;

    public MatchingService(MongoDbContext context, ILogger<MatchingService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<MatchingResult> CalculateMatchAsync(string jobId, string applicationId)
    {
        // Check for existing match result (prevent duplicate)
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

        // Build result object
        var matchResult = new MatchingResult
        {
            CandidateId = parsedResume?.CandidateId ?? string.Empty,
            JobId = jobId,
            JobApplicationId = applicationId,
            CreatedAt = DateTime.UtcNow
        };

        // If either parsed data is missing, save empty result and return
        if (parsedResume == null || parsedJob == null)
        {
            _logger.LogWarning(
                "Missing parsed data for matching — ParsedResume: {HasResume}, ParsedJob: {HasJob} (App: {AppId})",
                parsedResume != null, parsedJob != null, applicationId);

            await _context.MatchingResults.InsertOneAsync(matchResult);
            return matchResult;
        }

        try
        {
            // --- SKILL MATCHING ---
            var resumeSkills = parsedResume.Skills
                .Select(s => s.ToLower())
                .ToHashSet();

            var jobSkills = parsedJob.RequiredSkills
                .Select(s => s.ToLower())
                .ToList();

            var matchedSkills = jobSkills
                .Where(s => resumeSkills.Contains(s))
                .ToList();

            var missingSkills = jobSkills
                .Where(s => !resumeSkills.Contains(s))
                .ToList();

            // Map back to display names
            matchResult.MatchedSkills = matchedSkills
                .Select(s => GetDisplayName(s))
                .ToList();

            matchResult.MissingSkills = missingSkills
                .Select(s => GetDisplayName(s))
                .ToList();

            // SkillMatchScore = (matched / required) * 100
            matchResult.SkillMatchScore = jobSkills.Count > 0
                ? Math.Round((double)matchedSkills.Count / jobSkills.Count * 100, 1)
                : 0; // No required skills parsed = cannot score

            // --- EXPERIENCE MATCHING ---
            var resumeYears = parsedResume.ExperienceYears;
            var jobYears = parsedJob.RequiredExperienceYears;

            if (jobYears <= 0)
            {
                matchResult.ExperienceScore = 100; // No experience required
            }
            else if (resumeYears >= jobYears)
            {
                matchResult.ExperienceScore = 100;
            }
            else
            {
                matchResult.ExperienceScore = Math.Round((double)resumeYears / jobYears * 100, 1);
            }

            // --- TOTAL SCORE ---
            matchResult.TotalScore = Math.Round(
                (matchResult.SkillMatchScore * SkillWeight) +
                (matchResult.ExperienceScore * ExperienceWeight),
                1);

            _logger.LogInformation(
                "[MATCHING] App:{AppId} | Skills:{SkillScore}% ({Matched}/{Total}) | Exp:{ExpScore}% | Total:{TotalScore}%",
                applicationId, matchResult.SkillMatchScore,
                matchedSkills.Count, jobSkills.Count,
                matchResult.ExperienceScore, matchResult.TotalScore);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to calculate match for application {ApplicationId}", applicationId);
            // Leave scores at 0 — don't crash
        }

        await _context.MatchingResults.InsertOneAsync(matchResult);
        return matchResult;
    }

    /// <summary>
    /// Map a lowercase skill keyword back to its display name using ParsingKeywords.
    /// </summary>
    private static string GetDisplayName(string lowercaseSkill)
    {
        for (int i = 0; i < ParsingKeywords.SkillKeywords.Length; i++)
        {
            if (ParsingKeywords.SkillKeywords[i] == lowercaseSkill)
                return ParsingKeywords.SkillDisplayNames[i];
        }
        return lowercaseSkill; // fallback
    }
}
