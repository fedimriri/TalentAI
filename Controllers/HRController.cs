using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using TalentAI.Data;
using TalentAI.DTOs;
using TalentAI.Models;
using TalentAI.Services;

namespace TalentAI.Controllers;

[Route("hr")]
public class HRController : Controller
{
    private readonly IJobService _jobService;
    private readonly IUserService _userService;
    private readonly IJobParserService _jobParser;
    private readonly MongoDbContext _context;
    private readonly ILogger<HRController> _logger;

    public HRController(IJobService jobService, IUserService userService,
        IJobParserService jobParser, MongoDbContext context, ILogger<HRController> logger)
    {
        _jobService = jobService;
        _userService = userService;
        _jobParser = jobParser;
        _context = context;
        _logger = logger;
    }

    private bool IsHR()
    {
        return HttpContext.Session.GetString("Role") == "HR";
    }

    // A helper method wrapping the check to see if an HR MUST finish updating their profile.
    private async Task<bool> RequiresProfileUpdateAsync()
    {
        var hrId = HttpContext.Session.GetString("UserId");
        if (hrId == null) return false;

        var user = await _userService.GetHRByIdAsync(hrId);
        return user?.RequiresProfileUpdate ?? false;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        if (!IsHR()) return Redirect("/");
        if (await RequiresProfileUpdateAsync()) return RedirectToAction(nameof(UpdateProfile));

        var jobs = await _jobService.GetAllJobsAsync();
        return View(jobs);
    }

    [HttpGet("job/{id}")]
    public async Task<IActionResult> JobDetails(string id, 
        [FromQuery] double? minScore, 
        [FromQuery] string? skill, 
        [FromQuery] double? minExperience)
    {
        if (!IsHR()) return Redirect("/");
        if (await RequiresProfileUpdateAsync()) return RedirectToAction(nameof(UpdateProfile));

        var job = await _jobService.GetJobByIdAsync(id);
        if (job == null) return NotFound();

        var applicants = await _jobService.GetApplicationsForJobAsync(id);

        // Fetch matching results & parsed resumes for all applicants
        var matchScores = new Dictionary<string, MatchingResult>();
        var parsedResumes = new Dictionary<string, ParsedResume>();

        foreach (var app in Enumerable.Reverse(applicants).ToList()) // to avoid modifying collection while iterating if we remove
        {
            var match = await _context.MatchingResults
                .Find(m => m.JobApplicationId == app.Id)
                .FirstOrDefaultAsync();
                
            var parsed = await _context.ParsedResumes
                .Find(p => p.JobApplicationId == app.Id)
                .FirstOrDefaultAsync();

            // Apply Filters
            if (minScore.HasValue && (match == null || match.TotalScore < minScore.Value))
            {
                applicants.Remove(app);
                continue;
            }

            if (!string.IsNullOrWhiteSpace(skill) && 
                (match == null || !match.MatchedSkills.Any(s => s.Contains(skill, StringComparison.OrdinalIgnoreCase))))
            {
                applicants.Remove(app);
                continue;
            }

            if (minExperience.HasValue && (parsed == null || parsed.ExperienceYears < minExperience.Value))
            {
                applicants.Remove(app);
                continue;
            }

            if (match != null) matchScores[app.Id] = match;
            if (parsed != null) parsedResumes[app.Id] = parsed;
        }

        // Sort applicants by TotalScore descending
        var sortedApplicants = applicants
            .OrderByDescending(a => matchScores.ContainsKey(a.Id) ? matchScores[a.Id].TotalScore : 0)
            .ToList();

        ViewBag.Applicants = sortedApplicants;
        ViewBag.MatchScores = matchScores;
        
        // Pass back current filters for UI
        ViewBag.FilterMinScore = minScore;
        ViewBag.FilterSkill = skill;
        ViewBag.FilterMinExperience = minExperience;
        
        return View(job);
    }

    [HttpGet("add-job")]
    public async Task<IActionResult> AddJob()
    {
        if (!IsHR()) return Redirect("/");
        if (await RequiresProfileUpdateAsync()) return RedirectToAction(nameof(UpdateProfile));

        return View();
    }

    [HttpPost("add-job")]
    public async Task<IActionResult> AddJob(CreateJobDto dto)
    {
        if (!IsHR()) return Redirect("/");
        if (await RequiresProfileUpdateAsync()) return RedirectToAction(nameof(UpdateProfile));

        if (!ModelState.IsValid)
        {
            return View(dto);
        }

        var hrId = HttpContext.Session.GetString("UserId")!;
        var hrEmail = HttpContext.Session.GetString("Email")!;

        var job = await _jobService.CreateJobAsync(dto, hrId, hrEmail);

        // Parse the job description (non-blocking — errors are caught)
        try
        {
            await _jobParser.ParseAsync(job);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Job description parsing failed for job {JobId}", job.Id);
        }

        TempData["SuccessMessage"] = "Job created successfully!";
        
        return RedirectToAction("Index");
    }

    [HttpGet("application/{id}")]
    public async Task<IActionResult> ApplicationDetails(string id)
    {
        var role = HttpContext.Session.GetString("Role");
        if (role != "HR" && role != "Admin") return Redirect("/");

        var application = await _jobService.GetApplicationByIdAsync(id);
        if (application == null) return NotFound();

        var job = await _jobService.GetJobByIdAsync(application.JobId);
        ViewBag.JobTitle = job?.Title ?? "Unknown Job";

        var matchResult = await _context.MatchingResults
            .Find(m => m.JobApplicationId == id)
            .FirstOrDefaultAsync();
        ViewBag.MatchResult = matchResult;

        var feedback = await _context.HRFeedbacks
            .Find(f => f.ApplicationId == id)
            .FirstOrDefaultAsync();
        ViewBag.Feedback = feedback;

        return View(application);
    }

    [HttpPost("feedback/{applicationId}")]
    public async Task<IActionResult> SubmitFeedback(string applicationId, int rating, string comment)
    {
        var role = HttpContext.Session.GetString("Role");
        var userId = HttpContext.Session.GetString("UserId");
        if (role != "HR" && role != "Admin") return Redirect("/");

        var existing = await _context.HRFeedbacks
            .Find(f => f.ApplicationId == applicationId)
            .FirstOrDefaultAsync();

        if (existing != null)
        {
            var update = Builders<HRFeedback>.Update
                .Set(f => f.Rating, Math.Clamp(rating, 1, 5))
                .Set(f => f.Comment, comment ?? string.Empty)
                .Set(f => f.CreatedAt, DateTime.UtcNow);

            await _context.HRFeedbacks.UpdateOneAsync(f => f.Id == existing.Id, update);
            TempData["SuccessMessage"] = "Feedback updated successfully.";
        }
        else
        {
            var newFeedback = new HRFeedback
            {
                ApplicationId = applicationId,
                HRUserId = userId!,
                Rating = Math.Clamp(rating, 1, 5),
                Comment = comment ?? string.Empty,
                CreatedAt = DateTime.UtcNow
            };
            await _context.HRFeedbacks.InsertOneAsync(newFeedback);
            TempData["SuccessMessage"] = "Feedback saved successfully.";
        }

        return Redirect($"/hr/application/{applicationId}");
    }

    [HttpPost("application/update-status/{id}")]
    public async Task<IActionResult> UpdateStatus(string id, string newStatus)
    {
        var role = HttpContext.Session.GetString("Role");
        if (role != "HR" && role != "Admin") return Redirect("/");

        var success = await _jobService.UpdateApplicationStatusAsync(id, newStatus);

        if (success)
        {
            TempData["SuccessMessage"] = $"Application status updated to \"{newStatus}\".";
        }
        else
        {
            TempData["ErrorMessage"] = "Failed to update status. Invalid status value.";
        }

        return Redirect($"/hr/application/{id}");
    }

    [HttpGet("update-profile")]
    public async Task<IActionResult> UpdateProfile()
    {
        if (!IsHR()) return Redirect("/");

        var hrId = HttpContext.Session.GetString("UserId")!;
        var user = await _userService.GetHRByIdAsync(hrId);
        
        if (user == null) return Redirect("/");

        // If they already completed it, they don't *have* to be here, but they optionally could manually visit it later
        // Note: For now, if they don't require an update, let them proceed (it acts as their normal profile edit page).
        
        var dto = new HRProfileUpdateDto
        {
            FirstName = user.FirstName ?? "",
            LastName = user.LastName ?? "",
            MatriculeRH = user.MatriculeRH ?? "",
            Email = user.Email
            // Don't auto-fill the plain text password input field. Let them set a new mandatory one.
        };

        return View(dto);
    }

    [HttpPost("update-profile")]
    public async Task<IActionResult> UpdateProfile(HRProfileUpdateDto dto)
    {
        if (!IsHR()) return Redirect("/");

        if (!ModelState.IsValid)
        {
            return View(dto);
        }

        var hrId = HttpContext.Session.GetString("UserId")!;
        var success = await _userService.UpdateHRProfileAsync(hrId, dto);

        if (success)
        {
            // Sync Session just in case they changed their explicitly tracked email
            HttpContext.Session.SetString("Email", dto.Email);
            
            TempData["SuccessMessage"] = "Profile officially fully verified. You now have access to the Dashboard.";
            return RedirectToAction(nameof(Index));
        }

        ModelState.AddModelError("", "Failed to update profile.");
        return View(dto);
    }
}
