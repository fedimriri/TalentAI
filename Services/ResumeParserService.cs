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

    // Explicit experience patterns ("5 years", "2 years 11 months", "3 ans", "experience: 4", "2 yrs")
    private static readonly Regex YearMonthPattern = new(
        @"(\d+)\s*(?:years?|ans)\s*(?:and\s*)?(\d+)\s*(?:months?|mo|mois)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex[] ExplicitExperiencePatterns =
    {
        new(@"(\d+)\s*\+?\s*(years|year)", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"(\d+)\s*\+?\s*(ans)", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"experience\s*:?\s*(\d+)", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"(\d+)\s+yrs", RegexOptions.IgnoreCase | RegexOptions.Compiled),
    };

    // Date range pattern: captures optional month + year on both sides
    // e.g. "Oct 2023 – Jan 2026", "2023 - 2026", "Jan 2022 – Present"
    private static readonly Regex DateRangePattern = new(
        @"(?:(\w+)\s+)?(\d{4})\s*[-–—]\s*(?:(\w+)\s+)?(\d{4}|present|current|now|aujourd'hui)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // Month name → number mapping
    private static readonly Dictionary<string, int> MonthMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { "jan", 1 }, { "january", 1 },
        { "feb", 2 }, { "february", 2 },
        { "mar", 3 }, { "march", 3 },
        { "apr", 4 }, { "april", 4 },
        { "may", 5 },
        { "jun", 6 }, { "june", 6 },
        { "jul", 7 }, { "july", 7 },
        { "aug", 8 }, { "august", 8 },
        { "sep", 9 }, { "september", 9 },
        { "oct", 10 }, { "october", 10 },
        { "nov", 11 }, { "november", 11 },
        { "dec", 12 }, { "december", 12 },
    };

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
            }
            else
            {
                parsedResume.RawText = rawText;

                // Normalize text for parsing (lowercase + collapse whitespace)
                var normalizedText = rawText.ToLower();
                normalizedText = Regex.Replace(normalizedText, @"\s+", " ");

                _logger.LogInformation("[RESUME PARSE] RawText length: {Len}, first 200 chars: {Preview}",
                    rawText.Length, rawText.Substring(0, Math.Min(200, rawText.Length)));

                parsedResume.Skills = ExtractSkills(normalizedText);
                _logger.LogInformation("[RESUME PARSE] Skills found: {Skills}", string.Join(", ", parsedResume.Skills));

                parsedResume.ExperienceYears = ExtractExperienceYears(normalizedText);
                _logger.LogInformation("[RESUME PARSE] Final ExperienceYears: {Years}", parsedResume.ExperienceYears);

                parsedResume.Education = ExtractEducation(normalizedText);
                _logger.LogInformation("[RESUME PARSE] Education: {Edu}", parsedResume.Education);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse resume file: {FilePath}", filePath);
        }

        await _context.ParsedResumes.InsertOneAsync(parsedResume);
        return parsedResume;
    }

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

    private string ExtractTextFromDocx(string filePath)
    {
        using var doc = WordprocessingDocument.Open(filePath, false);
        var body = doc.MainDocumentPart?.Document?.Body;
        return body?.InnerText ?? string.Empty;
    }

    private static List<string> ExtractSkills(string normalizedText)
    {
        var found = new List<string>();
        for (int i = 0; i < ParsingKeywords.SkillKeywords.Length; i++)
        {
            if (normalizedText.Contains(ParsingKeywords.SkillKeywords[i]))
            {
                var displayName = ParsingKeywords.SkillDisplayNames[i];
                if (!found.Contains(displayName))
                    found.Add(displayName);
            }
        }
        return found;
    }

    /// <summary>
    /// Extract experience with month-level precision.
    /// Strategy 1: Explicit "X years" → returns MAX as whole number.
    /// Strategy 2: Date ranges with month parsing → SUM all durations (cap 20).
    /// </summary>
    private double ExtractExperienceYears(string normalizedText)
    {
        // --- STRATEGY 0: Combined "X years Y months" pattern (highest precision) ---
        var ymMatch = YearMonthPattern.Match(normalizedText);
        if (ymMatch.Success)
        {
            int.TryParse(ymMatch.Groups[1].Value, out var y);
            int.TryParse(ymMatch.Groups[2].Value, out var m);
            var combined = Math.Round(y + m / 12.0, 2);
            _logger.LogInformation("[EXPERIENCE DEBUG] Found year+month pattern: {Y}y {M}m = {Combined}", y, m, combined);
            return combined;
        }

        // --- STRATEGY 1: Explicit "X years" patterns ---
        int explicitMax = 0;
        foreach (var pattern in ExplicitExperiencePatterns)
        {
            foreach (Match match in pattern.Matches(normalizedText))
            {
                if (int.TryParse(match.Groups[1].Value, out var years) && years > explicitMax)
                    explicitMax = years;
            }
        }

        if (explicitMax > 0)
        {
            _logger.LogInformation("[EXPERIENCE DEBUG] Found explicit years: {Years}", explicitMax);
            return explicitMax;
        }

        // --- STRATEGY 2: Date range parsing with month precision ---
        var now = DateTime.UtcNow;
        var dateMatches = DateRangePattern.Matches(normalizedText);
        double totalYears = 0;
        var ranges = new List<string>();

        foreach (Match match in dateMatches)
        {
            // Parse start
            var startMonthStr = match.Groups[1].Value;
            if (!int.TryParse(match.Groups[2].Value, out var startYear)) continue;
            int startMonth = MonthMap.TryGetValue(startMonthStr, out var sm) ? sm : 1;

            // Parse end
            var endMonthStr = match.Groups[3].Value;
            var endYearStr = match.Groups[4].Value.ToLower();

            int endYear, endMonth;
            if (endYearStr is "present" or "current" or "now" or "aujourd'hui")
            {
                endYear = now.Year;
                endMonth = now.Month;
            }
            else if (int.TryParse(endYearStr, out endYear))
            {
                endMonth = MonthMap.TryGetValue(endMonthStr, out var em) ? em : 12;
            }
            else
            {
                continue;
            }

            // Sanity check
            if (startYear < 1970 || endYear > now.Year + 1) continue;
            var startDate = new DateTime(startYear, startMonth, 1);
            var endDate = new DateTime(endYear, endMonth, 1);
            if (endDate < startDate) continue;

            // Calculate months difference → years
            int monthsDiff = (endDate.Year - startDate.Year) * 12 + (endDate.Month - startDate.Month);
            double duration = Math.Round(monthsDiff / 12.0, 1);
            totalYears += duration;
            ranges.Add($"{startMonthStr} {startYear} → {endMonthStr} {endYear} = {duration}y");
        }

        // Cap at 20 years
        if (totalYears > 20) totalYears = 20;
        totalYears = Math.Round(totalYears, 1);

        if (ranges.Count > 0)
        {
            _logger.LogInformation("[EXPERIENCE DEBUG] Date ranges: [{Ranges}], total: {Total}y",
                string.Join(", ", ranges), totalYears);
        }
        else
        {
            _logger.LogInformation("[EXPERIENCE DEBUG] No explicit years or date ranges found.");
        }

        return totalYears;
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
