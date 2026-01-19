using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using ForumDyskusyjne.Data;
using ForumDyskusyjne.Models;
using System.Security.Claims;

namespace ForumDyskusyjne.Controllers
{
    [Route("api/announcements")]
    [ApiController]
    public class AnnouncementsApiController : ControllerBase
    {
        private readonly ForumDbContext _context;

        public AnnouncementsApiController(ForumDbContext context)
        {
            _context = context;
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdClaim, out var userId))
                return userId;
            return 0;
        }

        [AllowAnonymous]
        [HttpGet("active")]
        public async Task<IActionResult> GetAnnouncements()
        {
            try
            {
                var announcements = await _context.Announcements
                    .Include(a => a.CreatedByUser)
                    .Where(a => a.IsActive && (a.ExpiresAt == null || a.ExpiresAt > DateTime.UtcNow))
                    .OrderByDescending(a => a.CreatedAt)
                    .Select(a => new
                    {
                        a.Id,
                        a.Title,
                        a.Content,
                        a.IsActive,
                        a.CreatedAt,
                        a.ExpiresAt,
                        createdBy = a.CreatedByUser.Username
                    })
                    .ToListAsync();

                return Ok(announcements);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("all")]
        public async Task<IActionResult> GetAllAnnouncements()
        {
            try
            {
                var announcements = await _context.Announcements
                    .Include(a => a.CreatedByUser)
                    .Where(a => a.ExpiresAt == null || a.ExpiresAt > DateTime.UtcNow)
                    .OrderByDescending(a => a.CreatedAt)
                    .Select(a => new
                    {
                        a.Id,
                        a.Title,
                        a.Content,
                        a.IsActive,
                        a.CreatedAt,
                        a.ExpiresAt,
                        createdBy = a.CreatedByUser.Username
                    })
                    .ToListAsync();

                return Ok(announcements);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> CreateAnnouncement([FromBody] CreateAnnouncementRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                
                if (userId == 0)
                    return Unauthorized(new { error = "Użytkownik nie jest zalogowany" });

                if (string.IsNullOrWhiteSpace(request.Title))
                    return BadRequest(new { error = "Tytuł jest wymagany" });

                if (string.IsNullOrWhiteSpace(request.Content))
                    return BadRequest(new { error = "Treść jest wymagana" });

                var user = await _context.Users.FindAsync(userId);
                if (user == null || user.Role.ToString() != "Admin")
                    return Forbid();

                var announcement = new Announcement
                {
                    Title = request.Title.Trim(),
                    Content = request.Content.Trim(),
                    CreatedBy = userId,
                    IsActive = request.IsActive,
                    CreatedAt = DateTime.UtcNow,
                    ExpiresAt = request.ExpiresAt
                };

                _context.Announcements.Add(announcement);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetAnnouncements), new { id = announcement.Id }, new
                {
                    announcement.Id,
                    announcement.Title,
                    announcement.Content,
                    announcement.IsActive,
                    announcement.CreatedAt,
                    message = "Ogłoszenie zostało dodane"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateAnnouncement(int id, [FromBody] UpdateAnnouncementRequest request)
        {
            try
            {
                var announcement = await _context.Announcements.FindAsync(id);
                if (announcement == null)
                    return NotFound(new { error = "Ogłoszenie nie znalezione" });

                announcement.Title = request.Title?.Trim() ?? announcement.Title;
                announcement.Content = request.Content?.Trim() ?? announcement.Content;
                announcement.IsActive = request.IsActive ?? announcement.IsActive;
                announcement.ExpiresAt = request.ExpiresAt ?? announcement.ExpiresAt;

                _context.Announcements.Update(announcement);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Ogłoszenie zostało zaktualizowane" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAnnouncement(int id)
        {
            try
            {
                var announcement = await _context.Announcements.FindAsync(id);
                if (announcement == null)
                    return NotFound(new { error = "Ogłoszenie nie znalezione" });

                _context.Announcements.Remove(announcement);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Ogłoszenie zostało usunięte" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }

    public class CreateAnnouncementRequest
    {
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public DateTime? ExpiresAt { get; set; }
    }

    public class UpdateAnnouncementRequest
    {
        public string? Title { get; set; }
        public string? Content { get; set; }
        public bool? IsActive { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }
}