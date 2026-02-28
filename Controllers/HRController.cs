using Microsoft.AspNetCore.Mvc;
using TalentAI.DTOs;
using TalentAI.Services;

namespace TalentAI.Controllers;

[Route("hr")]
public class HRController : Controller
{
    private readonly IJobService _jobService;
    private readonly IUserService _userService;

    public HRController(IJobService jobService, IUserService userService)
    {
        _jobService = jobService;
        _userService = userService;
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
    public async Task<IActionResult> JobDetails(string id)
    {
        if (!IsHR()) return Redirect("/");
        if (await RequiresProfileUpdateAsync()) return RedirectToAction(nameof(UpdateProfile));

        var job = await _jobService.GetJobByIdAsync(id);
        if (job == null) return NotFound();

        var applicants = await _jobService.GetApplicationsForJobAsync(id);
        
        ViewBag.Applicants = applicants;
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

        await _jobService.CreateJobAsync(dto, hrId, hrEmail);
        TempData["SuccessMessage"] = "Job created successfully!";
        
        return RedirectToAction(nameof(Index));
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
