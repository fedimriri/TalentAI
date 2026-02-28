using Microsoft.AspNetCore.Mvc;
using TalentAI.DTOs;
using TalentAI.Services;
using TalentAI.Models;

namespace TalentAI.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] UserRegisterDto dto)
    {
        var user = await _authService.RegisterCandidateAsync(dto.Email, dto.Password);

        if (user == null)
            return BadRequest(new { message = "Email already exists." });

        return StatusCode(201, new
        {
            message = "Registration successful.",
            user = new
            {
                user.Id,
                user.Email,
                user.Role,
                user.CreatedAt
            }
        });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] UserLoginDto dto)
    {
        var user = await _authService.LoginAsync(dto.Email, dto.Password);

        if (user == null)
            return Unauthorized(new { message = "Invalid email or password." });

        // Set Session variables
        HttpContext.Session.SetString("UserId", user.Id);
        HttpContext.Session.SetString("Email", user.Email);
        HttpContext.Session.SetString("Role", user.Role);

        // Determine redirect url based on role
        string redirectUrl = "/candidate"; // default for Phase 2 implementation if active
        if (user.Role == "Admin")
        {
            redirectUrl = "/admin";
        }
        else if (user.Role == "HR")
        {
            redirectUrl = "/hr";
        }

        return Ok(new
        {
            message = "Login successful.",
            redirectUrl = redirectUrl,
            user = new
            {
                user.Id,
                user.Email,
                user.Role
            }
        });
    }
}
