using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace TalentAI.Models;

public class Job
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;

    public string Title { get; set; } = null!;
    public string Description { get; set; } = null!;
    public string Requirements { get; set; } = null!;
    public DateTime Deadline { get; set; }

    [BsonRepresentation(BsonType.ObjectId)]
    public string PostedByUserId { get; set; } = null!;
    
    public string PostedByEmail { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
