using TalentAI.Models;

namespace TalentAI.Services;

public interface IMatchingService
{
    Task<MatchingResult> CalculateMatchAsync(string jobId, string applicationId);
}
