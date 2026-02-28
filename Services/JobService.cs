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
}
