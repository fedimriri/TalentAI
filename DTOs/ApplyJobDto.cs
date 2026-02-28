using System.ComponentModel.DataAnnotations;

namespace TalentAI.DTOs;

public class ApplyJobDto
{
    [Required]
    public string JobId { get; set; } = null!;

    [Required]
    public IFormFile ResumeFile { get; set; } = null!;
}
