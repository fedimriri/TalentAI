using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace TalentAI.Models;

public class CandidateProfile
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;

    [BsonRepresentation(BsonType.ObjectId)]
    public string CandidateId { get; set; } = null!;

    public List<string> Skills { get; set; } = new();

    public double ExperienceYears { get; set; }

    public string Education { get; set; } = string.Empty;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
