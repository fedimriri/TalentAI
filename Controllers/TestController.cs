using Microsoft.AspNetCore.Mvc;
using TalentAI.Data;

namespace TalentAI.Controllers;

[ApiController]
[Route("api/test")]
public class TestController : ControllerBase
{
    private readonly MongoDbContext _context;

    public TestController(MongoDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public IActionResult TestConnection()
    {
        var collections = _context.Users.Database.ListCollectionNames().ToList();
        return Ok(new { message = "Mongo Connected", collections });
    }
}
