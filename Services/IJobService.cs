using TalentAI.DTOs;
using TalentAI.Models;

namespace TalentAI.Services;

public interface IJobService
{
    Task<List<Job>> GetAllJobsAsync();
    Task<Job?> GetJobByIdAsync(string jobId);
    Task<Job> CreateJobAsync(CreateJobDto dto, string hrUserId, string hrEmail);
    Task<List<JobApplication>> GetApplicationsForJobAsync(string jobId);
    Task<JobApplication?> ApplyForJobAsync(string jobId, string candidateId, string candidateEmail, string resumePath);
    Task<JobApplication?> GetApplicationByIdAsync(string applicationId);
}
