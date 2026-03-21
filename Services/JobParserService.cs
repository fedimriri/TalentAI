using System.Text.RegularExpressions;
using MongoDB.Driver;
using TalentAI.Data;
using TalentAI.Models;

namespace TalentAI.Services;

public class JobParserService : IJobParserService
{
    private readonly MongoDbContext _context;
    private readonly ILogger<JobParserService> _logger;

    // Reuse the SAME regex pattern as ResumeParserService
    private static readonly Regex ExperienceRegex = new(
        @"(\d+)\s*(\+)?\s*(years|year|ans)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public JobParserService(MongoDbContext context, ILogger<JobParserService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ParsedJob> ParseAsync(Job job)
    {
        // Prevent duplicate parsing for the same job
        var existing = await _context.ParsedJobs
            .Find(p => p.JobId == job.Id)
            .FirstOrDefaultAsync();

        if (existing != null)
        {
            _logger.LogInformation("ParsedJob already exists for job {JobId}, skipping.", job.Id);
            return existing;
        }

        var parsedJob = new ParsedJob
        {
            JobId = job.Id,
            CreatedAt = DateTime.UtcNow
        };

        try
        {
            // Combine Description + Requirements for full analysis
            var rawDescription = $"{job.Description} {job.Requirements}";
            parsedJob.RawDescription = rawDescription;

            // Normalize text: lowercase + collapse whitespace
            var normalizedText = rawDescription.ToLower();
            normalizedText = Regex.Replace(normalizedText, @"\s+", " ");

            // Extract required skills using shared keyword list
            parsedJob.RequiredSkills = ExtractSkills(normalizedText);

            // Extract required experience years
            parsedJob.RequiredExperienceYears = ExtractExperienceYears(normalizedText);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse job description for job {JobId}", job.Id);
            // Return default/empty parsed job — do not break the job creation flow
        }

        // Single insert point
        await _context.ParsedJobs.InsertOneAsync(parsedJob);
        return parsedJob;
    }

    /// <summary>
    /// Match normalized text against the shared skill keyword list.
    /// Returns display names (original casing) with no duplicates.
    /// </summary>
    private static List<string> ExtractSkills(string normalizedText)
    {
        var found = new List<string>();

        for (int i = 0; i < ParsingKeywords.SkillKeywords.Length; i++)
        {
            if (normalizedText.Contains(ParsingKeywords.SkillKeywords[i]))
            {
                var displayName = ParsingKeywords.SkillDisplayNames[i];
                if (!found.Contains(displayName))
                {
                    found.Add(displayName);
                }
            }
        }

        return found;
    }

    /// <summary>
    /// Extract required experience years using regex.
    /// Returns the FIRST valid match (typical for job descriptions).
    /// </summary>
    private static int ExtractExperienceYears(string normalizedText)
    {
        var match = ExperienceRegex.Match(normalizedText);
        if (!match.Success) return 0;

        return int.TryParse(match.Groups[1].Value, out var years) ? years : 0;
    }
}
