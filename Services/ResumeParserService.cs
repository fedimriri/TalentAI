using System.Text;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml.Packaging;
using MongoDB.Driver;
using TalentAI.Data;
using TalentAI.Models;
using UglyToad.PdfPig;

namespace TalentAI.Services;

public class ResumeParserService : IResumeParserService
{
    private readonly MongoDbContext _context;
    private readonly ILogger<ResumeParserService> _logger;

    // Education keywords to detect (lowercase for normalized comparison)
    private static readonly string[] EducationKeywords =
    {
        "bachelor", "master", "engineering", "degree",
        "phd", "doctorate", "licence", "ingénieur",
        "mba", "bsc", "msc", "b.sc", "m.sc",
        "computer science", "information technology"
    };

    // Explicit experience patterns ("5 years", "3 ans", "experience: 4", "2 yrs")
    private static readonly Regex[] ExplicitExperiencePatterns =
    {
        new(@"(\d+)\s*\+?\s*(years|year)", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"(\d+)\s*\+?\s*(ans)", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"experience\s*:?\s*(\d+)", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"(\d+)\s+yrs", RegexOptions.IgnoreCase | RegexOptions.Compiled),
    };

    // Date range pattern ("2023 - 2026", "Oct 2023 – Present", "2020 – 2022")
    private static readonly Regex DateRangePattern = new(
        @"(\b\d{4})\s*[-–—]\s*(\d{4}|present|current|aujourd'hui|now)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public ResumeParserService(MongoDbContext context, ILogger<ResumeParserService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ParsedResume> ParseAsync(string filePath, string candidateId, string applicationId)
    {
        // STEP 6: Prevent duplicate parsing for the same application
        var existing = await _context.ParsedResumes
            .Find(p => p.JobApplicationId == applicationId)
            .FirstOrDefaultAsync();

        if (existing != null)
        {
            _logger.LogInformation("ParsedResume already exists for application {ApplicationId}, skipping.", applicationId);
            return existing;
        }

        var parsedResume = new ParsedResume
        {
            CandidateId = candidateId,
            JobApplicationId = applicationId,
            CreatedAt = DateTime.UtcNow
        };

        try
        {
            // 1. Extract raw text from the file
            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            var rawText = extension switch
            {
                ".pdf" => ExtractTextFromPdf(filePath),
                ".docx" => ExtractTextFromDocx(filePath),
                _ => string.Empty
            };

            if (string.IsNullOrWhiteSpace(rawText))
            {
                _logger.LogWarning("No text extracted from resume file: {FilePath}", filePath);
                // Fall through to the single insert below (no double-insert)
            }
            else
            {
                // Store original raw text
                parsedResume.RawText = rawText;

                // Normalize text for parsing (lowercase + collapse whitespace)
                var normalizedText = rawText.ToLower();
                normalizedText = Regex.Replace(normalizedText, @"\s+", " ");

                _logger.LogInformation("[RESUME PARSE] RawText length: {Len}, first 200 chars: {Preview}",
                    rawText.Length, rawText.Substring(0, Math.Min(200, rawText.Length)));

                // Extract skills from normalized text
                parsedResume.Skills = ExtractSkills(normalizedText);
                _logger.LogInformation("[RESUME PARSE] Skills found: {Skills}", string.Join(", ", parsedResume.Skills));

                // Extract experience years from normalized text
                parsedResume.ExperienceYears = ExtractExperienceYears(normalizedText);
                _logger.LogInformation("[RESUME PARSE] Final ExperienceYears: {Years}", parsedResume.ExperienceYears);

                // Extract education from normalized text
                parsedResume.Education = ExtractEducation(normalizedText);
                _logger.LogInformation("[RESUME PARSE] Education: {Edu}", parsedResume.Education);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse resume file: {FilePath}", filePath);
            // Return default/empty parsed resume — do not break the application flow
        }

        // Single insert point — no double-insert possible
        await _context.ParsedResumes.InsertOneAsync(parsedResume);
        return parsedResume;
    }

    /// <summary>
    /// Extract text from a PDF file using PdfPig.
    /// </summary>
    private string ExtractTextFromPdf(string filePath)
    {
        var sb = new StringBuilder();

        using var document = PdfDocument.Open(filePath);
        foreach (var page in document.GetPages())
        {
            sb.AppendLine(page.Text);
        }

        return sb.ToString();
    }

    /// <summary>
    /// Extract text from a DOCX file using OpenXml.
    /// </summary>
    private string ExtractTextFromDocx(string filePath)
    {
        using var doc = WordprocessingDocument.Open(filePath, false);
        var body = doc.MainDocumentPart?.Document?.Body;
        return body?.InnerText ?? string.Empty;
    }

    /// <summary>
    /// Match normalized resume text against predefined skill keywords.
    /// Uses simple Contains on already-lowered text for reliable matching.
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
    /// Extract years of experience using a two-strategy approach:
    /// 1. Explicit patterns ("5 years") → use MAX if found
    /// 2. Date ranges ("2023 – 2026") → SUM all durations (cap 20)
    /// Explicit patterns take priority. Falls back to date ranges.
    /// </summary>
    private int ExtractExperienceYears(string normalizedText)
    {
        // --- STRATEGY 1: Explicit "X years" patterns ---
        int explicitMax = 0;
        foreach (var pattern in ExplicitExperiencePatterns)
        {
            foreach (Match match in pattern.Matches(normalizedText))
            {
                if (int.TryParse(match.Groups[1].Value, out var years) && years > explicitMax)
                {
                    explicitMax = years;
                }
            }
        }

        if (explicitMax > 0)
        {
            _logger.LogInformation("[EXPERIENCE DEBUG] Found explicit years: {Years}", explicitMax);
            return explicitMax;
        }

        // --- STRATEGY 2: Date range parsing ("2023 – 2026") ---
        var currentYear = DateTime.UtcNow.Year;
        var dateMatches = DateRangePattern.Matches(normalizedText);
        int totalFromDates = 0;
        var ranges = new List<string>();

        foreach (Match match in dateMatches)
        {
            if (!int.TryParse(match.Groups[1].Value, out var startYear))
                continue;

            int endYear;
            var endGroup = match.Groups[2].Value.ToLower();
            if (endGroup == "present" || endGroup == "current" || endGroup == "now" || endGroup == "aujourd'hui")
            {
                endYear = currentYear;
            }
            else if (!int.TryParse(endGroup, out endYear))
            {
                continue;
            }

            // Sanity: years must be reasonable (1970–future)
            if (startYear < 1970 || endYear < startYear || endYear > currentYear + 1)
                continue;

            var duration = endYear - startYear;
            totalFromDates += duration;
            ranges.Add($"{startYear}-{endYear}={duration}y");
        }

        // Cap at 20 years to avoid absurd sums
        if (totalFromDates > 20) totalFromDates = 20;

        if (ranges.Count > 0)
        {
            _logger.LogInformation("[EXPERIENCE DEBUG] Found date ranges: [{Ranges}], total: {Total}y",
                string.Join(", ", ranges), totalFromDates);
        }
        else
        {
            _logger.LogInformation("[EXPERIENCE DEBUG] No explicit years or date ranges found.");
        }

        return totalFromDates;
    }

    /// <summary>
    /// Extract education level by finding known keywords in the normalized text.
    /// Returns the first matching line/segment containing a keyword.
    /// </summary>
    private static string ExtractEducation(string normalizedText)
    {
        // Split by common sentence boundaries for line-level scanning
        var lines = normalizedText.Split(new[] { '\n', '.', ';' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            foreach (var keyword in EducationKeywords)
            {
                if (line.Contains(keyword))
                {
                    return line.Trim();
                }
            }
        }

        return string.Empty;
    }
}
