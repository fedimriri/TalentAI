using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace TalentAI.Models;

public class MatchingResult
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;

    [BsonRepresentation(BsonType.ObjectId)]
    public string CandidateId { get; set; } = null!;

    [BsonRepresentation(BsonType.ObjectId)]
    public string JobId { get; set; } = null!;

    [BsonRepresentation(BsonType.ObjectId)]
    public string JobApplicationId { get; set; } = null!;

    // ATS Scores (0–100)
    public double SkillScore { get; set; }
    public double ExperienceScore { get; set; }
    public double EducationScore { get; set; }
    public double TotalScore { get; set; }

    // Weights used for this calculation
    public double SkillWeight { get; set; }
    public double ExperienceWeight { get; set; }
    public double EducationWeight { get; set; }

    // Skill breakdown
    public List<string> MatchedSkills { get; set; } = new();
    public List<string> MissingSkills { get; set; } = new();

    // Readable JSON breakdown for HR
    public string ScoreBreakdown { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Keep backward compat — old documents had SkillMatchScore
    [BsonIgnoreIfNull]
    public double? SkillMatchScore
    {
        get => null;
        set { if (value.HasValue) SkillScore = value.Value; }
    }
}
