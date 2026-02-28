using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace TalentAI.Models;

public class JobApplication
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;

    [BsonRepresentation(BsonType.ObjectId)]
    public string JobId { get; set; } = null!;

    [BsonRepresentation(BsonType.ObjectId)]
    public string CandidateId { get; set; } = null!;
    
    public string CandidateEmail { get; set; } = null!;
    
    [BsonRepresentation(BsonType.ObjectId)]
    public string ResumeId { get; set; } = null!;

    public DateTime AppliedAt { get; set; } = DateTime.UtcNow;
}
