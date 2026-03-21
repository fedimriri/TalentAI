using TalentAI.Models;

namespace TalentAI.Services;

public interface IJobParserService
{
    Task<ParsedJob> ParseAsync(Job job);
}
