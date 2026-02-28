using MongoDB.Driver;
using TalentAI.Data;
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
}
