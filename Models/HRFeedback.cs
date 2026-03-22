using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace TalentAI.Models;

public class HRFeedback
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;

    [BsonRepresentation(BsonType.ObjectId)]
    public string ApplicationId { get; set; } = null!;

    [BsonRepresentation(BsonType.ObjectId)]
    public string HRUserId { get; set; } = null!;

    public int Rating { get; set; } // 1-5 stars

    public string Comment { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
