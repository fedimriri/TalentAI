using MongoDB.Driver;
using TalentAI.Data;
using TalentAI.DTOs;
using TalentAI.Models;

namespace TalentAI.Services;

public class UserService : IUserService
{
    private readonly MongoDbContext _context;

    public UserService(MongoDbContext context)
    {
        _context = context;
    }

    public async Task<List<User>> GetAllUsersAsync()
    {
        return await _context.Users.Find(_ => true).ToListAsync();
    }

    public async Task<User?> CreateHRAsync(string email, string password, string firstName, string lastName, string matriculeRh)
    {
        // Check if email already exists
        var existingUser = await _context.Users
            .Find(u => u.Email == email)
            .FirstOrDefaultAsync();

        if (existingUser != null)
            return null; // or throw an exception

        var hrUser = new User
        {
            Email = email,
            Password = password,
            FirstName = firstName,
            LastName = lastName,
            MatriculeRH = matriculeRh,
            Role = "HR",
            RequiresProfileUpdate = true,
            CreatedAt = DateTime.UtcNow
        };

        await _context.Users.InsertOneAsync(hrUser);
        return hrUser;
    }

    public async Task<bool> DeleteUserAsync(string id)
    {
        var result = await _context.Users.DeleteOneAsync(u => u.Id == id);
        return result.DeletedCount > 0;
    }

    public async Task<User?> GetHRByIdAsync(string id)
    {
        return await _context.Users.Find(u => u.Id == id && u.Role == "HR").FirstOrDefaultAsync();
    }

    public async Task<bool> UpdateHRProfileAsync(string id, HRProfileUpdateDto dto)
    {
        var update = Builders<User>.Update
            .Set(u => u.FirstName, dto.FirstName)
            .Set(u => u.LastName, dto.LastName)
            .Set(u => u.MatriculeRH, dto.MatriculeRH)
            .Set(u => u.Email, dto.Email)
            .Set(u => u.Password, dto.Password)
            .Set(u => u.RequiresProfileUpdate, false);

        var result = await _context.Users.UpdateOneAsync(u => u.Id == id && u.Role == "HR", update);
        return result.ModifiedCount > 0;
    }

    public async Task<User?> GetCandidateByIdAsync(string id)
    {
        return await _context.Users.Find(u => u.Id == id && u.Role == "Candidate").FirstOrDefaultAsync();
    }

    public async Task<List<CandidateJobDto>> GetAppliedJobsForCandidateAsync(string candidateId)
    {
        // Find all applications for this candidate
        var applications = await _context.JobApplications
            .Find(a => a.CandidateId == candidateId)
            .SortByDescending(a => a.AppliedAt)
            .ToListAsync();

        var dtos = new List<CandidateJobDto>();

        foreach (var app in applications)
        {
            // Fetch the corresponding job to get the title
            var job = await _context.Jobs.Find(j => j.Id == app.JobId).FirstOrDefaultAsync();
            if (job != null)
            {
                dtos.Add(new CandidateJobDto
                {
                    ApplicationId = app.Id!,
                    JobId = job.Id!,
                    JobTitle = job.Title,
                    AppliedAt = app.AppliedAt,
                    Status = app.Status ?? "Under Review"
                });
            }
        }

        return dtos;
    }
}
