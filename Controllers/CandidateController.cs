using Microsoft.AspNetCore.Mvc;
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

    [HttpPost("apply/{id}")]
    public async Task<IActionResult> Apply(string id)
    {
        if (!IsCandidate()) return Redirect("/");

        var candidateId = HttpContext.Session.GetString("UserId")!;
        var candidateEmail = HttpContext.Session.GetString("Email")!;

        var result = await _jobService.ApplyForJobAsync(id, candidateId, candidateEmail);

        if (result == null)
        {
            TempData["ErrorMessage"] = "You have already applied for this position.";
        }
        else
        {
            TempData["SuccessMessage"] = "Application submitted successfully.";
        }

        return Redirect($"/candidate/job/{id}");
    }
}
