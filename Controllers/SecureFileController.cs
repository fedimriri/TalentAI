using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using TalentAI.Data;
using TalentAI.Models;

namespace TalentAI.Controllers;

[Route("secure")]
public class SecureFileController : Controller
{
    private readonly MongoDbContext _context;
    private readonly ILogger<SecureFileController> _logger;
    private readonly IWebHostEnvironment _env;

    public SecureFileController(MongoDbContext context, ILogger<SecureFileController> logger,
        IWebHostEnvironment env)
    {
        _context = context;
        _logger = logger;
        _env = env;
    }

    [HttpGet("resume/{applicationId}")]
    public async Task<IActionResult> Resume(string applicationId)
    {
        var role = HttpContext.Session.GetString("Role");
        var userId = HttpContext.Session.GetString("UserId");

        if (string.IsNullOrEmpty(role) || string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning("[SECURE] Unauthenticated resume access attempt for app {AppId}", applicationId);
            return Forbid();
        }

        var application = await _context.JobApplications
            .Find(a => a.Id == applicationId)
            .FirstOrDefaultAsync();

        if (application == null)
        {
            return NotFound("Application not found.");
        }

        // Authorization: HR, Admin, or owning candidate
        bool authorized = role is "HR" or "Admin"
            || (role == "Candidate" && application.CandidateId == userId);

        if (!authorized)
        {
            _logger.LogWarning(
                "[SECURE] Unauthorized resume access: User {UserId} (Role: {Role}) tried to access App {AppId}",
                userId, role, applicationId);
            return Forbid();
        }

        if (string.IsNullOrEmpty(application.ResumeFilePath))
        {
            return NotFound("No resume file found for this application.");
        }

        // Resolve file path
        var filePath = application.ResumeFilePath;
        if (filePath.StartsWith("/"))
        {
            filePath = Path.Combine(_env.WebRootPath, filePath.TrimStart('/'));
        }

        if (!System.IO.File.Exists(filePath))
        {
            _logger.LogWarning("[SECURE] Resume file not found on disk: {Path}", filePath);
            return NotFound("Resume file not found on disk.");
        }

        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        var contentType = extension switch
        {
            ".pdf" => "application/pdf",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".doc" => "application/msword",
            _ => "application/octet-stream"
        };

        var fileName = Path.GetFileName(filePath);
        _logger.LogInformation("[SECURE] Serving resume for App {AppId} to User {UserId} ({Role})",
            applicationId, userId, role);

        return PhysicalFile(filePath, contentType, fileName);
    }
}
