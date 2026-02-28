namespace TalentAI.DTOs;

public class CandidateJobDto
{
    public string ApplicationId { get; set; } = null!;
    public string JobId { get; set; } = null!;
    public string JobTitle { get; set; } = null!;
    public DateTime AppliedAt { get; set; }
    public string Status { get; set; } = "Under Review";
}
