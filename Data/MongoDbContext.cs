using MongoDB.Driver;
using TalentAI.Configurations;
using TalentAI.Models;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

namespace TalentAI.Data;

public class MongoDbContext
{
    private readonly IMongoDatabase _database;

    public MongoDbContext(IOptions<MongoSettings> settings, ILogger<MongoDbContext> logger)
    {
        var connectionString = settings.Value.ConnectionString;
        var databaseName = settings.Value.DatabaseName;

        // Log connection info without exposing credentials
        var safeConnectionInfo = connectionString.Contains("@")
            ? connectionString.Substring(connectionString.IndexOf('@'))
            : connectionString;
        logger.LogInformation("Connecting to MongoDB: ...{SafeConnectionInfo}, Database: {DatabaseName}",
            safeConnectionInfo, databaseName);

        try
        {
            var clientSettings = MongoClientSettings.FromConnectionString(connectionString);
            clientSettings.ServerSelectionTimeout = TimeSpan.FromSeconds(10);
            clientSettings.ConnectTimeout = TimeSpan.FromSeconds(10);
            clientSettings.RetryReads = true;
            clientSettings.RetryWrites = true;

            var client = new MongoClient(clientSettings);
            _database = client.GetDatabase(databaseName);

            logger.LogInformation("MongoDB client initialized successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to initialize MongoDB client. Check your connection string.");
            throw;
        }
    }

    public IMongoCollection<User> Users =>
        _database.GetCollection<User>("Users");

    public IMongoCollection<Resume> Resumes =>
        _database.GetCollection<Resume>("Resumes");
    public IMongoCollection<Job> Jobs => _database.GetCollection<Job>("Jobs");
    public IMongoCollection<JobApplication> JobApplications => _database.GetCollection<JobApplication>("JobApplications");
    public IMongoCollection<ParsedResume> ParsedResumes => _database.GetCollection<ParsedResume>("ParsedResumes");
    public IMongoCollection<ParsedJob> ParsedJobs => _database.GetCollection<ParsedJob>("ParsedJobs");
    public IMongoCollection<MatchingResult> MatchingResults => _database.GetCollection<MatchingResult>("MatchingResults");
    public IMongoCollection<CandidateProfile> CandidateProfiles => _database.GetCollection<CandidateProfile>("CandidateProfiles");
    public IMongoCollection<HRFeedback> HRFeedbacks => _database.GetCollection<HRFeedback>("HRFeedbacks");
}
