using MongoDB.Driver;
using TalentAI.Data;
using TalentAI.Models;

namespace TalentAI.Services;

public class AuthService : IAuthService
{
    private readonly MongoDbContext _context;

    public AuthService(MongoDbContext context)
    {
        _context = context;
    }

    public async Task<User?> RegisterCandidateAsync(string email, string password)
    {
        // Check if email already exists
        var existingUser = await _context.Users
            .Find(u => u.Email == email)
            .FirstOrDefaultAsync();

        if (existingUser != null)
            return null;

        var user = new User
        {
            Email = email,
            Password = password,
            Role = "Candidate",
            CreatedAt = DateTime.UtcNow
        };

        await _context.Users.InsertOneAsync(user);
        return user;
    }

    public async Task<User?> LoginAsync(string email, string password)
    {
        var user = await _context.Users
            .Find(u => u.Email == email && u.Password == password)
            .FirstOrDefaultAsync();

        return user;
    }

    public async Task SeedAdminAsync()
    {
        var adminExists = await _context.Users
            .Find(u => u.Email == "admin@talentai.com")
            .AnyAsync();

        if (!adminExists)
        {
            var admin = new User
            {
                Email = "admin@talentai.com",
                Password = "admin123",
                Role = "Admin",
                CreatedAt = DateTime.UtcNow
            };

            await _context.Users.InsertOneAsync(admin);
        }
    }
}
