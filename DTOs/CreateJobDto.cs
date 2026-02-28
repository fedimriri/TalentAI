using System.ComponentModel.DataAnnotations;

namespace TalentAI.DTOs;

public class CreateJobDto
{
    [Required]
    public string Title { get; set; } = null!;

    [Required]
    public string Description { get; set; } = null!;

    [Required]
    public string Requirements { get; set; } = null!;

    [Required]
    public DateTime Deadline { get; set; }
}
