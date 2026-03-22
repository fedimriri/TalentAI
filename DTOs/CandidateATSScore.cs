namespace TalentAI.DTOs;

public class CandidateATSScore
{
    // Base scores (0–100)
    public double SkillScore { get; set; }
    public double ExperienceScore { get; set; }
    public double EducationScore { get; set; }
    public double TotalScore { get; set; }

    // Weights used
    public double SkillWeight { get; set; }
    public double ExperienceWeight { get; set; }
    public double EducationWeight { get; set; }

    // Skill breakdown
    public List<string> MatchedSkills { get; set; } = new();
    public List<string> MissingSkills { get; set; } = new();
    public List<string> ResumeSkills { get; set; } = new();

    // Parsed data
    public double ExperienceYears { get; set; }
    public string Education { get; set; } = string.Empty;

    // AI-enhanced analysis (from Groq)
    public string AiAnalysis { get; set; } = string.Empty;
    public bool AiEnhanced { get; set; }

    // Error state
    public string? ErrorMessage { get; set; }
}
