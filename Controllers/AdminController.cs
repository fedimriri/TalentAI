using Microsoft.AspNetCore.Mvc;
using TalentAI.DTOs;
using TalentAI.Services;

namespace TalentAI.Controllers;

[Route("admin")]
public class AdminController : Controller
{
    private readonly IUserService _userService;

    public AdminController(IUserService userService)
    {
        _userService = userService;
    }

    // A helper method to check if the current user is an Admin
    private bool IsAdmin()
    {
        var role = HttpContext.Session.GetString("Role");
        return role == "Admin";
    }

    [HttpGet("")]
    public IActionResult Index()
    {
        if (!IsAdmin()) return Redirect("/");

        ViewBag.Email = HttpContext.Session.GetString("Email");
        ViewBag.Role = HttpContext.Session.GetString("Role");
        return View();
    }

    [HttpGet("users")]
    public async Task<IActionResult> Users()
    {
        if (!IsAdmin()) return Redirect("/");

        var currentUserId = HttpContext.Session.GetString("UserId");
        var users = await _userService.GetAllUsersAsync();
        
        // Exclude the current admin from the list
        users = users.Where(u => u.Id != currentUserId).ToList();

        return View(users);
    }

    [HttpGet("add-manager")]
    public IActionResult AddManager()
    {
        if (!IsAdmin()) return Redirect("/");
        return View();
    }

    [HttpPost("add-manager")]
    public async Task<IActionResult> AddManager(CreateHRDto dto)
    {
        if (!IsAdmin()) return Redirect("/");

        if (!ModelState.IsValid)
        {
            return View(dto);
        }

        var result = await _userService.CreateHRAsync(dto.Email, dto.Password, dto.FirstName, dto.LastName, dto.MatriculeRH);
        
        if (result == null)
        {
            ModelState.AddModelError("Email", "Email already exists.");
            return View(dto);
        }

        TempData["SuccessMessage"] = $"HR account for {dto.Email} created successfully!";
        return RedirectToAction(nameof(AddManager));
    }

    [HttpPost("delete-user/{id}")]
    public async Task<IActionResult> DeleteUser(string id)
    {
        if (!IsAdmin()) return Redirect("/");

        var currentUserId = HttpContext.Session.GetString("UserId");
        if (id == currentUserId)
        {
            TempData["ErrorMessage"] = "You cannot delete your own account.";
            return RedirectToAction(nameof(Users));
        }

        var success = await _userService.DeleteUserAsync(id);
        if (success)
        {
            TempData["SuccessMessage"] = "User successfully deleted.";
        }
        else
        {
            TempData["ErrorMessage"] = "Failed to delete user.";
        }

        return RedirectToAction(nameof(Users));
    }
}
