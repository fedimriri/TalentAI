using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace TalentAI.Models;

public class Resume
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;

    public string CandidateName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string FilePath { get; set; } = null!;
    public string ExtractedText { get; set; } = string.Empty;
    public List<string> Skills { get; set; } = new();
    public double? AiScore { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}
