using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ForumDyskusyjne.Data;
using ForumDyskusyjne.Models;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.ComponentModel.DataAnnotations;

namespace ForumDyskusyjne.Controllers
{
    public class ForumUpdateDto
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }
    }

    [Route("api/mod")]
    [ApiController]
    [Authorize(Roles = "Moderator")]
    public class ModApiController : ControllerBase
    {
        private readonly ForumDbContext _context;
        public ModApiController(ForumDbContext context)
        {
            _context = context;
        }

        // GET: api/mod/forums
        [HttpGet("forums")]
        public async Task<ActionResult<IEnumerable<object>>> GetAssignedForums()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out var userId))
                return Unauthorized();

            var forumIds = await _context.ForumModerators
                .Where(fm => fm.UserId == userId)
                .Select(fm => fm.ForumId)
                .ToListAsync();

            var forums = await _context.Forums
                .Where(f => forumIds.Contains(f.Id))
                .Select(f => new { 
                    f.Id, 
                    f.Name,
                    f.Description,
                    ThreadCount = f.Threads.Count(),
                    MessageCount = f.Threads.SelectMany(t => t.Messages).Count()
                })
                .ToListAsync();

            return Ok(forums);
        }

        // GET: api/mod/users
        [HttpGet("users")]
        public async Task<ActionResult<object>> GetAssignedForumUsers([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = null)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out var userId))
                return Unauthorized();

            var forumIds = await _context.ForumModerators
                .Where(fm => fm.UserId == userId)
                .Select(fm => fm.ForumId)
                .ToListAsync();

            if (!forumIds.Any())
            {
                return Ok(new { users = new List<object>(), total = 0 });
            }

            var userIdsInModeratedForums = await _context.Messages
                .Include(m => m.Thread)
                .Where(m => forumIds.Contains(m.Thread.ForumId))
                .Select(m => m.AuthorId)
                .Distinct()
                .ToListAsync();
            
            var query = _context.Users
                .Where(u => userIdsInModeratedForums.Contains(u.Id) && u.Id != userId);

            // Apply search filter
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(u => u.Username.Contains(search) || u.Email.Contains(search));
            }

            var totalUsers = await query.CountAsync();

            var users = await query
                .OrderBy(u => u.Username)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new { 
                    u.Id, 
                    u.Username, 
                    u.Email, 
                    Status = u.IsBanned ? "blocked" : "active",
                    Role = u.Role.ToString(),
                    u.AvatarUrl,
                    u.CreatedAt,
                    u.LastActivityAt
                })
                .ToListAsync();

            return Ok(new { users = users, total = totalUsers });
        }

        // GET: api/mod/threads
        [HttpGet("threads")]
        public async Task<ActionResult<IEnumerable<object>>> GetAssignedForumThreads()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out var userId))
                return Unauthorized();

            var forumIds = await _context.ForumModerators
                .Where(fm => fm.UserId == userId)
                .Select(fm => fm.ForumId)
                .ToListAsync();

            var threads = await _context.Threads
                .Where(t => forumIds.Contains(t.ForumId))
                .Include(t => t.Author)
                .Include(t => t.Forum)
                .Select(t => new
                {
                    t.Id,
                    t.Title,
                    AuthorName = t.Author.Username,
                    ForumName = t.Forum.Name,
                    t.RepliesCount,
                    t.Views,
                    t.CreatedAt
                })
                .ToListAsync();

            return Ok(threads);
        }

        [HttpPost("users/{userId}/block")]
        public async Task<IActionResult> BlockUser(int userId)
        {
            var moderatorIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(moderatorIdStr, out var moderatorId))
                return Unauthorized();

            // Get forums moderated by the current user
            var moderatedForumIds = await _context.ForumModerators
                .Where(fm => fm.UserId == moderatorId)
                .Select(fm => fm.ForumId)
                .ToListAsync();

            if (!moderatedForumIds.Any())
                return Forbid("You do not moderate any forums.");

            // Find the user to block
            var userToBlock = await _context.Users.FindAsync(userId);
            if (userToBlock == null)
                return NotFound("User not found.");

            // Prevent blocking admins
            if (userToBlock.Role == UserRole.Admin)
            {
                return Forbid("Administrators cannot be blocked.");
            }

            // Authorization: Check if the user has posted in any of the moderated forums
            var isUserInModeratedForums = await _context.Messages
                .Include(m => m.Thread)
                .AnyAsync(m => m.AuthorId == userId && moderatedForumIds.Contains(m.Thread.ForumId));

            if (!isUserInModeratedForums)
                return Forbid("You do not have permission to block this user as they have not participated in your moderated forums.");

            // Block the user
            userToBlock.IsBanned = true;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpPost("users/{userId}/unblock")]
        public async Task<IActionResult> UnblockUser(int userId)
        {
            var moderatorIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(moderatorIdStr, out var moderatorId))
                return Unauthorized();

            // Get forums moderated by the current user
            var moderatedForumIds = await _context.ForumModerators
                .Where(fm => fm.UserId == moderatorId)
                .Select(fm => fm.ForumId)
                .ToListAsync();

            if (!moderatedForumIds.Any())
                return Forbid("You do not moderate any forums.");

            // Find the user to unblock
            var userToUnblock = await _context.Users.FindAsync(userId);
            if (userToUnblock == null)
                return NotFound("User not found.");

            // Prevent unblocking admins by mods (as a safety measure)
            if (userToUnblock.Role == UserRole.Admin)
            {
                return Forbid("This action cannot be performed on an administrator account.");
            }

            // Unblock the user
            userToUnblock.IsBanned = false;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("threads/{threadId}")]
        public async Task<IActionResult> DeleteThread(int threadId)
        {
            var moderatorIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(moderatorIdStr, out var moderatorId))
                return Unauthorized();

            // Get forums moderated by the current user
            var moderatedForumIds = await _context.ForumModerators
                .Where(fm => fm.UserId == moderatorId)
                .Select(fm => fm.ForumId)
                .ToListAsync();

            if (!moderatedForumIds.Any())
                return Forbid("You do not moderate any forums.");

            // Find the thread to delete
            var threadToDelete = await _context.Threads.FindAsync(threadId);

            if (threadToDelete == null)
                return NotFound("Thread not found.");

            // Authorization: Check if the thread is in a moderated forum
            if (!moderatedForumIds.Contains(threadToDelete.ForumId))
                return Forbid("You do not have permission to delete this thread.");

            // Delete the thread
            _context.Threads.Remove(threadToDelete);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // GET: api/mod/forums/{forumId}
        [HttpGet("forums/{forumId}")]
        public async Task<ActionResult<Forum>> GetForum(int forumId)
        {
            var moderatorIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(moderatorIdStr, out var moderatorId))
                return Unauthorized();

            var isModerator = await _context.ForumModerators
                .AnyAsync(fm => fm.UserId == moderatorId && fm.ForumId == forumId);

            if (!isModerator)
                return Forbid("You do not have permission to view this forum.");

            var forum = await _context.Forums.FindAsync(forumId);

            if (forum == null)
                return NotFound();

            return forum;
        }

        // PUT: api/mod/forums/{forumId}
        [HttpPut("forums/{forumId}")]
        public async Task<IActionResult> UpdateForum(int forumId, ForumUpdateDto forumUpdateDto)
        {
            var moderatorIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(moderatorIdStr, out var moderatorId))
                return Unauthorized();

            var isModerator = await _context.ForumModerators
                .AnyAsync(fm => fm.UserId == moderatorId && fm.ForumId == forumId);

            if (!isModerator)
                return Forbid("You do not have permission to edit this forum.");

            var forumToUpdate = await _context.Forums.FindAsync(forumId);
            if (forumToUpdate == null)
                return NotFound();

            forumToUpdate.Name = forumUpdateDto.Name;
            forumToUpdate.Description = forumUpdateDto.Description;

            _context.Entry(forumToUpdate).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Forums.Any(e => e.Id == forumId))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }
    }
}
