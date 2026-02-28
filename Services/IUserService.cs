using TalentAI.Models;

namespace TalentAI.Services;

public interface IUserService
{
    Task<List<User>> GetAllUsersAsync();
    Task<User?> CreateHRAsync(string email, string password, string firstName, string lastName, string matriculeRh);
    Task<bool> DeleteUserAsync(string id);
}
