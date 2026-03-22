using TalentAI.DTOs;

namespace TalentAI.Services;

public interface IAtsScoringService
{
    Task<CandidateATSScore> ComputeScoreAsync(string filePath, string candidateId);
}
