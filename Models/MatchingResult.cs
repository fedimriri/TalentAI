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

    public double SkillMatchScore { get; set; }

    public double ExperienceScore { get; set; }

    public double TotalScore { get; set; }

    public List<string> MatchedSkills { get; set; } = new();

    public List<string> MissingSkills { get; set; } = new();

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
