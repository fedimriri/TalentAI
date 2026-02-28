using TalentAI.Models;

namespace TalentAI.Services;

public interface IAuthService
{
    Task<User?> RegisterCandidateAsync(string email, string password);
    Task<User?> LoginAsync(string email, string password);
    Task SeedAdminAsync();
}
