using TalentAI.Models;

namespace TalentAI.Services;

public interface IResumeParserService
{
    Task<ParsedResume> ParseAsync(string filePath, string candidateId, string applicationId);
}
