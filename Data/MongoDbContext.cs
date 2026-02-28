using MongoDB.Driver;
using TalentAI.Configurations;
using TalentAI.Models;
using Microsoft.Extensions.Options;

namespace TalentAI.Data;

public class MongoDbContext
{
    private readonly IMongoDatabase _database;

    public MongoDbContext(IOptions<MongoSettings> settings)
    {
        var client = new MongoClient(settings.Value.ConnectionString);
        _database = client.GetDatabase(settings.Value.DatabaseName);
    }

    public IMongoCollection<User> Users =>
        _database.GetCollection<User>("Users");

    public IMongoCollection<Resume> Resumes =>
        _database.GetCollection<Resume>("Resumes");
    public IMongoCollection<Job> Jobs => _database.GetCollection<Job>("Jobs");
    public IMongoCollection<JobApplication> JobApplications => _database.GetCollection<JobApplication>("JobApplications");
}
