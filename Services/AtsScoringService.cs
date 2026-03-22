using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml.Packaging;
using Microsoft.Extensions.Options;
using TalentAI.Configurations;
using TalentAI.DTOs;
using TalentAI.Models;
using UglyToad.PdfPig;

namespace TalentAI.Services;

public class AtsScoringService : IAtsScoringService
{
    private readonly ILogger<AtsScoringService> _logger;
    private readonly HttpClient _httpClient;
    private readonly string _groqApiKey;

    // ATS weights
    private const double SkillWeight = 0.5;
    private const double ExperienceWeight = 0.3;
    private const double EducationWeight = 0.2;

    // Groq API
    private const string GroqEndpoint = "https://api.groq.com/openai/v1/chat/completions";
    private const string GroqModel = "llama-3.1-8b-instant";

    // Experience regex patterns
    private static readonly Regex YearMonthPattern = new(
        @"(\d+)\s*(?:years?|ans)\s*(?:and\s*)?(\d+)\s*(?:months?|mo|mois)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex[] ExperiencePatterns =
    {
        new(@"(\d+)\s*\+?\s*(years|year)", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"(\d+)\s*\+?\s*(ans)", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"experience\s*:?\s*(\d+)", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"(\d+)\s+yrs", RegexOptions.IgnoreCase | RegexOptions.Compiled),
    };

    private static readonly Regex DateRangePattern = new(
        @"(?:(\w+)\s+)?(\d{4})\s*[-–—]\s*(?:(\w+)\s+)?(\d{4}|present|current|now|aujourd'hui)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Dictionary<string, int> MonthMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { "jan", 1 }, { "january", 1 }, { "feb", 2 }, { "february", 2 },
        { "mar", 3 }, { "march", 3 }, { "apr", 4 }, { "april", 4 },
        { "may", 5 }, { "jun", 6 }, { "june", 6 },
        { "jul", 7 }, { "july", 7 }, { "aug", 8 }, { "august", 8 },
        { "sep", 9 }, { "september", 9 }, { "oct", 10 }, { "october", 10 },
        { "nov", 11 }, { "november", 11 }, { "dec", 12 }, { "december", 12 },
    };

    private static readonly string[] EducationKeywords =
    {
        "bachelor", "master", "engineering", "degree",
        "phd", "doctorate", "licence", "ingénieur",
        "mba", "bsc", "msc", "b.sc", "m.sc",
        "computer science", "information technology"
    };

    public AtsScoringService(ILogger<AtsScoringService> logger, HttpClient httpClient, IOptions<AISettings> aiSettings)
    {
        _logger = logger;
        _httpClient = httpClient;
        _groqApiKey = aiSettings.Value.GroqApiKey;
    }

    public async Task<CandidateATSScore> ComputeScoreAsync(string filePath, string candidateId)
    {
        var result = new CandidateATSScore
        {
            SkillWeight = SkillWeight,
            ExperienceWeight = ExperienceWeight,
            EducationWeight = EducationWeight
        };

        try
        {
            // 1. Extract raw text
            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            var rawText = extension switch
            {
                ".pdf" => ExtractTextFromPdf(filePath),
                ".docx" => ExtractTextFromDocx(filePath),
                _ => string.Empty
            };

            if (string.IsNullOrWhiteSpace(rawText))
            {
                result.ErrorMessage = "Could not extract text from the uploaded file.";
                return result;
            }

            var normalizedText = Regex.Replace(rawText.ToLower(), @"\s+", " ");

            // 2. Extract structured data
            result.ResumeSkills = ExtractSkills(normalizedText);
            result.ExperienceYears = ExtractExperienceYears(normalizedText);
            result.Education = ExtractEducation(normalizedText);

            // 3. Compute base scores (no job context — general assessment)
            result.SkillScore = result.ResumeSkills.Count > 0
                ? Math.Min(Math.Round(result.ResumeSkills.Count / 5.0 * 100, 1), 100)
                : 0;

            result.ExperienceScore = result.ExperienceYears > 0
                ? Math.Min(Math.Round(result.ExperienceYears / 5.0 * 100, 1), 100)
                : 0;

            result.EducationScore = ComputeEducationScore(result.Education);

            result.MatchedSkills = result.ResumeSkills;

            // 4. Try Groq AI enhancement
            try
            {
                var aiResult = await CallGroqAsync(rawText, result);
                if (aiResult != null)
                {
                    result.AiAnalysis = aiResult;
                    result.AiEnhanced = true;
                    _logger.LogInformation("[ATS AI] Groq analysis completed for candidate {CandidateId}", candidateId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[ATS AI] Groq API call failed, using base scoring only.");
                result.AiAnalysis = "AI analysis unavailable — using keyword-based scoring.";
            }

            // 5. Compute total
            result.TotalScore = Math.Round(
                (result.SkillScore * SkillWeight) +
                (result.ExperienceScore * ExperienceWeight) +
                (result.EducationScore * EducationWeight),
                1);

            _logger.LogInformation(
                "[ATS SCORE] Candidate:{CandidateId} Skills:{Skill} Exp:{Exp} Edu:{Edu} Total:{Total} AI:{AI}",
                candidateId, result.SkillScore, result.ExperienceScore,
                result.EducationScore, result.TotalScore, result.AiEnhanced);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ATS scoring failed for candidate {CandidateId}", candidateId);
            result.ErrorMessage = "An error occurred while analyzing your resume. Please try again.";
        }

        return result;
    }

    private async Task<string?> CallGroqAsync(string resumeText, CandidateATSScore baseScores)
    {
        // Limit text to first 3000 chars to stay within token limits
        var truncatedText = resumeText.Length > 3000 ? resumeText[..3000] : resumeText;

        var prompt = $@"Analyze this resume and provide a brief ATS assessment. Be concise (max 150 words).

Resume text:
{truncatedText}

Current parsed data:
- Skills detected: {string.Join(", ", baseScores.ResumeSkills)}
- Experience: {baseScores.ExperienceYears} years
- Education: {baseScores.Education}

Provide:
1. Overall impression (1 sentence)
2. Key strengths (2-3 bullet points)
3. Areas to improve (2-3 bullet points)
4. Suggested missing skills to add";

        var requestBody = new
        {
            model = GroqModel,
            messages = new[]
            {
                new { role = "system", content = "You are an ATS (Applicant Tracking System) resume analyzer. Be concise and actionable." },
                new { role = "user", content = prompt }
            },
            max_tokens = 300,
            temperature = 0.3
        };

        var json = JsonSerializer.Serialize(requestBody);
        var request = new HttpRequestMessage(HttpMethod.Post, GroqEndpoint)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        request.Headers.Add("Authorization", $"Bearer {_groqApiKey}");

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(responseJson);

        var content = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        return content;
    }

    private static double ComputeEducationScore(string education)
    {
        var edu = education.ToLower();
        if (string.IsNullOrWhiteSpace(edu)) return 40;

        if (edu.Contains("phd") || edu.Contains("doctorat") || edu.Contains("doctorate")) return 100;
        if (edu.Contains("master") || edu.Contains("engineering") || edu.Contains("ingénieur") ||
            edu.Contains("mba") || edu.Contains("msc") || edu.Contains("m.sc")) return 80;
        if (edu.Contains("bachelor") || edu.Contains("licence") ||
            edu.Contains("bsc") || edu.Contains("b.sc") || edu.Contains("degree")) return 60;
        return 40;
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

    private double ExtractExperienceYears(string normalizedText)
    {
        // Strategy 0: Combined "X years Y months"
        var ymMatch = YearMonthPattern.Match(normalizedText);
        if (ymMatch.Success)
        {
            int.TryParse(ymMatch.Groups[1].Value, out var y);
            int.TryParse(ymMatch.Groups[2].Value, out var m);
            return Math.Round(y + m / 12.0, 2);
        }

        // Strategy 1: Explicit patterns
        int explicitMax = 0;
        foreach (var pattern in ExperiencePatterns)
            foreach (Match match in pattern.Matches(normalizedText))
                if (int.TryParse(match.Groups[1].Value, out var yv) && yv > explicitMax)
                    explicitMax = yv;

        if (explicitMax > 0) return explicitMax;

        // Strategy 2: Date ranges with month precision
        var now = DateTime.UtcNow;
        double total = 0;

        foreach (Match match in DateRangePattern.Matches(normalizedText))
        {
            var startMonthStr = match.Groups[1].Value;
            if (!int.TryParse(match.Groups[2].Value, out var startYear)) continue;
            int startMonth = MonthMap.TryGetValue(startMonthStr, out var sm) ? sm : 1;

            var endMonthStr = match.Groups[3].Value;
            var endYearStr = match.Groups[4].Value.ToLower();

            int endYear, endMonth;
            if (endYearStr is "present" or "current" or "now" or "aujourd'hui")
            { endYear = now.Year; endMonth = now.Month; }
            else if (int.TryParse(endYearStr, out endYear))
            { endMonth = MonthMap.TryGetValue(endMonthStr, out var em) ? em : 12; }
            else continue;

            if (startYear < 1970 || endYear > now.Year + 1) continue;
            var start = new DateTime(startYear, startMonth, 1);
            var end = new DateTime(endYear, endMonth, 1);
            if (end < start) continue;

            int months = (end.Year - start.Year) * 12 + (end.Month - start.Month);
            total += Math.Round(months / 12.0, 1);
        }

        return Math.Min(Math.Round(total, 1), 20);
    }

    private static string ExtractEducation(string normalizedText)
    {
        var lines = normalizedText.Split(new[] { '\n', '.', ';' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
            foreach (var keyword in EducationKeywords)
                if (line.Contains(keyword))
                    return line.Trim();
        return string.Empty;
    }

    private static string ExtractTextFromPdf(string filePath)
    {
        var sb = new StringBuilder();
        using var document = PdfDocument.Open(filePath);
        foreach (var page in document.GetPages())
            sb.AppendLine(page.Text);
        return sb.ToString();
    }

    private static string ExtractTextFromDocx(string filePath)
    {
        using var doc = WordprocessingDocument.Open(filePath, false);
        var body = doc.MainDocumentPart?.Document?.Body;
        return body?.InnerText ?? string.Empty;
    }
}
