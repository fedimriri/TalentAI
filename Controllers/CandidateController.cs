using Microsoft.AspNetCore.Mvc;
using TalentAI.DTOs;
using TalentAI.Services;

namespace TalentAI.Controllers;

[Route("candidate")]
public class CandidateController : Controller
{
    private readonly IUserService _userService;
    private readonly IJobService _jobService;

    public CandidateController(IUserService userService, IJobService jobService)
    {
        _userService = userService;
        _jobService = jobService;
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
}
