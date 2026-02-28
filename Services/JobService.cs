using MongoDB.Driver;
using TalentAI.Data;
using TalentAI.DTOs;
using TalentAI.Models;

namespace TalentAI.Services;

public class JobService : IJobService
{
    private readonly MongoDbContext _context;

    public JobService(MongoDbContext context)
    {
        _context = context;
    }

    public async Task<List<Job>> GetAllJobsAsync()
    {
        return await _context.Jobs.Find(_ => true)
            .SortByDescending(j => j.CreatedAt)
            .ToListAsync();
    }

    public async Task<Job?> GetJobByIdAsync(string jobId)
    {
        return await _context.Jobs.Find(j => j.Id == jobId).FirstOrDefaultAsync();
    }

    public async Task<Job> CreateJobAsync(CreateJobDto dto, string hrUserId, string hrEmail)
    {
        var job = new Job
        {
            Title = dto.Title,
            Description = dto.Description,
            Requirements = dto.Requirements,
            Deadline = dto.Deadline,
            PostedByUserId = hrUserId,
            PostedByEmail = hrEmail,
            CreatedAt = DateTime.UtcNow
        };

        await _context.Jobs.InsertOneAsync(job);
        return job;
    }

    public async Task<List<JobApplication>> GetApplicationsForJobAsync(string jobId)
    {
        return await _context.JobApplications.Find(a => a.JobId == jobId)
            .SortByDescending(a => a.AppliedAt)
            .ToListAsync();
    }

    public async Task<JobApplication?> ApplyForJobAsync(string jobId, string candidateId, string candidateEmail, string resumePath)
    {
        // Check if candidate already applied for this job
        var existingApp = await _context.JobApplications
            .Find(a => a.JobId == jobId && a.CandidateId == candidateId)
            .FirstOrDefaultAsync();

        if (existingApp != null)
        {
            return null; // Already applied
        }

        var application = new JobApplication
        {
            JobId = jobId,
            CandidateId = candidateId,
            CandidateEmail = candidateEmail,
            ResumeFilePath = resumePath,
            AppliedAt = DateTime.UtcNow,
            Status = "Under Review"
        };

        await _context.JobApplications.InsertOneAsync(application);
        return application;
    }

    public async Task<JobApplication?> GetApplicationByIdAsync(string applicationId)
    {
        return await _context.JobApplications
            .Find(a => a.Id == applicationId)
            .FirstOrDefaultAsync();
    }
}
