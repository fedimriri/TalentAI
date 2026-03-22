using Microsoft.AspNetCore.Mvc;
using TalentAI.DTOs;
using TalentAI.Models;
using TalentAI.Services;

namespace TalentAI.Controllers;

[Route("candidate")]
public class CandidateController : Controller
{
    private readonly IUserService _userService;
    private readonly IJobService _jobService;
    private readonly IResumeParserService _resumeParser;
    private readonly IMatchingService _matchingService;
    private readonly IAtsScoringService _atsScoringService;
    private readonly ILogger<CandidateController> _logger;

    public CandidateController(IUserService userService, IJobService jobService,
        IResumeParserService resumeParser, IMatchingService matchingService,
        IAtsScoringService atsScoringService, ILogger<CandidateController> logger)
    {
        _userService = userService;
        _jobService = jobService;
        _resumeParser = resumeParser;
        _matchingService = matchingService;
        _atsScoringService = atsScoringService;
        _logger = logger;
    }

    private bool IsCandidate()
    {
        return HttpContext.Session.GetString("Role") == "Candidate";
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        if (!IsCandidate()) return Redirect("/");

        var candidateId = HttpContext.Session.GetString("UserId")!;
        
        var user = await _userService.GetCandidateByIdAsync(candidateId);
        if (user == null) return Redirect("/");

        var appliedJobs = await _userService.GetAppliedJobsForCandidateAsync(candidateId);

        ViewBag.Candidate = user;
        return View(appliedJobs);
    }

    [HttpGet("feed")]
    public async Task<IActionResult> Feed()
    {
        if (!IsCandidate()) return Redirect("/");

        var jobs = await _jobService.GetAllJobsAsync();
        return View(jobs);
    }

    [HttpGet("job/{id}")]
    public async Task<IActionResult> JobDetails(string id)
    {
        if (!IsCandidate()) return Redirect("/");

        var job = await _jobService.GetJobByIdAsync(id);
        if (job == null) return NotFound();

        var candidateId = HttpContext.Session.GetString("UserId")!;
        var appliedJobs = await _userService.GetAppliedJobsForCandidateAsync(candidateId);
        
        // Pass info if already applied
        ViewBag.HasApplied = appliedJobs.Any(a => a.JobId == id);
        
        return View(job);
    }

    [HttpGet("job/apply/{id}")]
    public async Task<IActionResult> Apply(string id)
    {
        if (!IsCandidate()) return Redirect("/");

        var job = await _jobService.GetJobByIdAsync(id);
        if (job == null) return NotFound();

        var dto = new ApplyJobDto { JobId = id };
        ViewBag.JobTitle = job.Title;

        return View("ApplyJob", dto);
    }

    [HttpPost("job/apply/{id}")]
    public async Task<IActionResult> Apply(string id, ApplyJobDto dto)
    {
        if (!IsCandidate()) return Redirect("/");

        if (id != dto.JobId) return BadRequest("Job ID mismatch.");

        if (dto.ResumeFile == null || dto.ResumeFile.Length == 0)
        {
            ModelState.AddModelError("ResumeFile", "Please upload a valid resume file.");
            var job = await _jobService.GetJobByIdAsync(id);
            ViewBag.JobTitle = job?.Title ?? "Unknown Job";
            return View("ApplyJob", dto);
        }

        // Validate File Extension/Type
        var extension = Path.GetExtension(dto.ResumeFile.FileName).ToLowerInvariant();
        if (extension != ".pdf" && extension != ".docx")
        {
            ModelState.AddModelError("ResumeFile", "Only PDF and DOCX files are allowed.");
            var job = await _jobService.GetJobByIdAsync(id);
            ViewBag.JobTitle = job?.Title ?? "Unknown Job";
            return View("ApplyJob", dto);
        }

        // Validate Size (5MB = 5 * 1024 * 1024 bytes)
        if (dto.ResumeFile.Length > 5 * 1024 * 1024)
        {
            ModelState.AddModelError("ResumeFile", "File size cannot exceed 5MB.");
            var job = await _jobService.GetJobByIdAsync(id);
            ViewBag.JobTitle = job?.Title ?? "Unknown Job";
            return View("ApplyJob", dto);
        }

        var candidateId = HttpContext.Session.GetString("UserId")!;
        var candidateEmail = HttpContext.Session.GetString("Email")!;

        // Save File to wwwroot/uploads physically
        var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
        if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

        var uniqueFileName = Guid.NewGuid().ToString() + extension;
        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await dto.ResumeFile.CopyToAsync(stream);
        }

        // Convert backend physical path references to logical web paths pointing inside standard wwwroot static routes
        var relativePath = "/uploads/" + uniqueFileName;

        var result = await _jobService.ApplyForJobAsync(id, candidateId, candidateEmail, relativePath);

        if (result == null)
        {
            // Already applied technically
            TempData["ErrorMessage"] = "You have already applied for this position.";
            return Redirect($"/candidate/job/{id}");
        }

        // Parse the uploaded resume (non-blocking — errors are caught)
        try
        {
            var physicalPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", relativePath.TrimStart('/'));

            if (!System.IO.File.Exists(physicalPath))
            {
                _logger.LogWarning("Resume file not found at {Path}, skipping parsing.", physicalPath);
            }
            else
            {
                await _resumeParser.ParseAsync(physicalPath, candidateId, result.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Resume parsing failed for application {ApplicationId}", result.Id);
        }

        // Calculate candidate-job match score (non-blocking)
        try
        {
            await _matchingService.CalculateMatchAsync(id, result.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Matching calculation failed for application {ApplicationId}", result.Id);
        }

        TempData["SuccessMessage"] = "Application submitted successfully.";
        return Redirect("/candidate");
    }

    [HttpGet("application/edit/{id}")]
    public async Task<IActionResult> EditApplication(string id)
    {
        if (!IsCandidate()) return Redirect("/");

        var candidateId = HttpContext.Session.GetString("UserId")!;
        var app = await _jobService.GetApplicationByIdAsync(id);

        if (app == null || app.CandidateId != candidateId) return NotFound();
        if (app.Status == "Approved")
        {
            TempData["ErrorMessage"] = "Cannot modify an approved application.";
            return Redirect("/candidate");
        }

        var job = await _jobService.GetJobByIdAsync(app.JobId);
        ViewBag.JobTitle = job?.Title ?? "Unknown Job";
        ViewBag.CurrentResume = app.ResumeFilePath;

        return View(app);
    }

    [HttpPost("application/edit/{id}")]
    public async Task<IActionResult> EditApplication(string id, IFormFile ResumeFile)
    {
        if (!IsCandidate()) return Redirect("/");

        var candidateId = HttpContext.Session.GetString("UserId")!;
        var app = await _jobService.GetApplicationByIdAsync(id);

        if (app == null || app.CandidateId != candidateId) return NotFound();
        if (app.Status == "Approved")
        {
            TempData["ErrorMessage"] = "Cannot modify an approved application.";
            return Redirect("/candidate");
        }

        if (ResumeFile == null || ResumeFile.Length == 0)
        {
            TempData["ErrorMessage"] = "Please upload a valid resume file.";
            return Redirect($"/candidate/application/edit/{id}");
        }

        var extension = Path.GetExtension(ResumeFile.FileName).ToLowerInvariant();
        if (extension != ".pdf" && extension != ".docx")
        {
            TempData["ErrorMessage"] = "Only PDF and DOCX files are allowed.";
            return Redirect($"/candidate/application/edit/{id}");
        }

        if (ResumeFile.Length > 5 * 1024 * 1024)
        {
            TempData["ErrorMessage"] = "File size cannot exceed 5MB.";
            return Redirect($"/candidate/application/edit/{id}");
        }

        var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
        if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

        var uniqueFileName = Guid.NewGuid().ToString() + extension;
        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await ResumeFile.CopyToAsync(stream);
        }

        var relativePath = "/uploads/" + uniqueFileName;
        await _jobService.ReplaceResumeAsync(id, relativePath);

        TempData["SuccessMessage"] = "Resume replaced successfully.";
        return Redirect("/candidate");
    }

    [HttpPost("application/delete/{id}")]
    public async Task<IActionResult> DeleteApplication(string id)
    {
        if (!IsCandidate()) return Redirect("/");

        var candidateId = HttpContext.Session.GetString("UserId")!;
        var app = await _jobService.GetApplicationByIdAsync(id);

        if (app == null || app.CandidateId != candidateId) return NotFound();
        if (app.Status == "Approved")
        {
            TempData["ErrorMessage"] = "Cannot delete an approved application.";
            return Redirect("/candidate");
        }

        await _jobService.DeleteApplicationAsync(id);

        TempData["SuccessMessage"] = "Application deleted successfully.";
        return Redirect("/candidate");
    }

    // --- ATS Score Tool ---

    [HttpGet("ats-score")]
    public IActionResult AtsScore()
    {
        if (!IsCandidate()) return Redirect("/");
        return View();
    }

    [HttpPost("ats-score")]
    public async Task<IActionResult> AtsScorePost(IFormFile ResumeFile)
    {
        if (!IsCandidate()) return Redirect("/");

        if (ResumeFile == null || ResumeFile.Length == 0)
        {
            ViewBag.Error = "Please upload a valid resume file.";
            return View("AtsScore");
        }

        var extension = Path.GetExtension(ResumeFile.FileName).ToLowerInvariant();
        if (extension != ".pdf" && extension != ".docx")
        {
            ViewBag.Error = "Only PDF and DOCX files are allowed.";
            return View("AtsScore");
        }

        if (ResumeFile.Length > 5 * 1024 * 1024)
        {
            ViewBag.Error = "File size cannot exceed 5MB.";
            return View("AtsScore");
        }

        // Save to temp location
        var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + extension);
        try
        {
            using (var stream = new FileStream(tempPath, FileMode.Create))
            {
                await ResumeFile.CopyToAsync(stream);
            }

            var candidateId = HttpContext.Session.GetString("UserId") ?? "anonymous";
            var result = await _atsScoringService.ComputeScoreAsync(tempPath, candidateId);

            ViewBag.Result = result;
            ViewBag.FileName = ResumeFile.FileName;
            return View("AtsScore");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ATS scoring failed");
            ViewBag.Error = "An error occurred while processing your resume. Please try again.";
            return View("AtsScore");
        }
        finally
        {
            if (System.IO.File.Exists(tempPath))
                System.IO.File.Delete(tempPath);
        }
    }
}
