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

    // Multiple regex patterns for robust experience extraction
    private static readonly Regex[] ExperiencePatterns =
    {
        new(@"(\d+)\s*\+?\s*(years|year)", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"(\d+)\s*\+?\s*(ans)", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"experience\s*:?\s*(\d+)", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"(\d+)\s+yrs", RegexOptions.IgnoreCase | RegexOptions.Compiled),
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
                _logger.LogInformation("[RESUME PARSE] ExperienceYears: {Years}", parsedResume.ExperienceYears);

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
    /// Extract years of experience using multiple regex patterns.
    /// Loops through all patterns and returns the MAX value found.
    /// </summary>
    private static int ExtractExperienceYears(string normalizedText)
    {
        int maxYears = 0;

        foreach (var pattern in ExperiencePatterns)
        {
            foreach (Match match in pattern.Matches(normalizedText))
            {
                if (int.TryParse(match.Groups[1].Value, out var years) && years > maxYears)
                {
                    maxYears = years;
                }
            }
        }

        return maxYears;
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
