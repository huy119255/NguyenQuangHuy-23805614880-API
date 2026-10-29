
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NguyenQuangHuy_23805614880_API.Data;

namespace NguyenQuangHuy_23805614880_API.Controllers;

[ApiController]
[Route("api/database")]
public class DatabaseController : ControllerBase
{
    private readonly AppDbContext _db;

    public DatabaseController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet("health")]
    public async Task<IActionResult> Health()
    {
        bool connected = await _db.Database.CanConnectAsync();

        if (!connected)
            return StatusCode(503, new { status = "error" });

        return Ok(new {
            status = "ok",
            database = "connected"
        });
    }
}
