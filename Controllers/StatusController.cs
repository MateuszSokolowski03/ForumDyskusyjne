using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ForumDyskusyjne.Data;

namespace ForumDyskusyjne.Controllers
{
    [Route("api/status")]
    [ApiController]
    public class StatusController : ControllerBase
    {
        private readonly ForumDbContext _context;

        public StatusController(ForumDbContext context)
        {
            _context = context;
        }

        [HttpGet("")]
        public async Task<IActionResult> GetStatus()
        {
            try
            {
                // Test połączenia z bazą danych
                var canConnect = await _context.Database.CanConnectAsync();
                
                if (canConnect)
                {
                    // Pobierz podstawowe statystyki
                    var stats = new
                    {
                        totalUsers = await _context.Users.CountAsync(),
                        totalCategories = await _context.Categories.CountAsync(),
                        totalForums = await _context.Forums.CountAsync(),
                        totalThreads = await _context.Threads.CountAsync(),
                        totalMessages = await _context.Messages.CountAsync()
                    };

                    return Ok(new
                    {
                        status = "OK",
                        database = "Connected",
                        timestamp = DateTime.UtcNow,
                        stats
                    });
                }
                else
                {
                    return StatusCode(503, new
                    {
                        status = "Error",
                        database = "Disconnected",
                        timestamp = DateTime.UtcNow,
                        error = "Cannot connect to database"
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = "Error",
                    database = "Error",
                    timestamp = DateTime.UtcNow,
                    error = ex.Message
                });
            }
        }
    }
}
