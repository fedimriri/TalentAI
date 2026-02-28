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
    public async Task<IActionResult> TestConnection()
    {
        var collections = new List<string>();
        using var cursor = await _context.Users.Database.ListCollectionNamesAsync();
        while (await cursor.MoveNextAsync())
        {
            collections.AddRange(cursor.Current);
        }
        return Ok(new { message = "Mongo Connected", collections });
    }
}
