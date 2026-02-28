using TalentAI.Models;
using TalentAI.DTOs;

namespace TalentAI.Services;

public interface IUserService
{
    Task<List<User>> GetAllUsersAsync();
    Task<User?> CreateHRAsync(string email, string password, string firstName, string lastName, string matriculeRh);
    Task<bool> DeleteUserAsync(string id);
    Task<User?> GetHRByIdAsync(string id);
    Task<bool> UpdateHRProfileAsync(string id, HRProfileUpdateDto dto);
}
