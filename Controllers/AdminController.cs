using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ForumDyskusyjne.Data;
using ForumDyskusyjne.Models;

namespace ForumDyskusyjne.Controllers
{
    [Route("api/admin")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private readonly ForumDbContext _context;

        public AdminController(ForumDbContext context)
        {
            _context = context;
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            try
            {
                var stats = new
                {
                    totalUsers = await _context.Users.CountAsync(),
                    totalCategories = await _context.Categories.CountAsync(),
                    totalForums = await _context.Forums.CountAsync(),
                    totalThreads = await _context.Threads.CountAsync(),
                    totalMessages = await _context.Messages.CountAsync(),
                    totalBannedWords = await _context.BannedWords.CountAsync(),
                    recentUsers = await _context.Users
                        .OrderByDescending(u => u.CreatedAt)
                        .Take(5)
                        .Select(u => new
                        {                        u.Id,
                        u.Username,
                        u.Email,
                        u.CreatedAt,
                        IsActive = !u.IsBanned
                        })
                        .ToListAsync()
                };

                return Ok(stats);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("users")]
        public async Task<IActionResult> GetUsers(int page = 1, int pageSize = 10, string search = "", string role = "", string status = "")
        {
            try
            {
                var query = _context.Users.Include(u => u.CurrentRank).AsQueryable();

                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(u => u.Username.Contains(search) || u.Email.Contains(search));
                }

                if (!string.IsNullOrEmpty(role))
                {
                    switch (role.ToLower())
                    {
                        case "admin":
                            query = query.Where(u => u.Role == UserRole.Admin);
                            break;
                        case "moderator":
                            query = query.Where(u => u.Role == UserRole.Moderator);
                            break;
                        case "user":
                            query = query.Where(u => u.Role == UserRole.User);
                            break;
                    }
                }

                if (!string.IsNullOrEmpty(status))
                {
                    switch (status.ToLower())
                    {
                        case "active":
                            query = query.Where(u => !u.IsBanned);
                            break;
                        case "banned":
                            query = query.Where(u => u.IsBanned);
                            break;
                    }
                }

                var totalUsers = await query.CountAsync();
                var users = await query
                    .OrderByDescending(u => u.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(u => new
                    {
                        u.Id,
                        u.Username,
                        u.Email,
                        u.CreatedAt,
                        LastActiveAt = u.LastActivityAt,
                        IsActive = !u.IsBanned,
                        IsAdmin = u.Role == UserRole.Admin,
                        IsModerator = u.Role == UserRole.Moderator,
                        MessageCount = u.PostCount,
                        ThreadCount = u.Threads.Count(),
                        Rank = u.CurrentRank != null ? u.CurrentRank.Name : "Brak rangi"
                    })
                    .ToListAsync();

                return Ok(new
                {
                    users,
                    totalUsers,
                    totalPages = (int)Math.Ceiling((double)totalUsers / pageSize),
                    currentPage = page,
                    pageSize
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("categories")]
        public async Task<IActionResult> GetCategories()
        {
            try
            {
                var categories = await _context.Categories
                    .Include(c => c.Forums)
                    .OrderBy(c => c.SortOrder)
                    .Select(c => new
                    {
                        c.Id,
                        c.Name,
                        c.Description,
                        c.SortOrder,
                        IsActive = true, // Category nie ma właściwości IsActive
                        c.CreatedAt,
                        ForumCount = c.Forums.Count,
                        ThreadCount = c.Forums.SelectMany(f => f.Threads).Count(),
                        MessageCount = c.Forums.SelectMany(f => f.Threads).SelectMany(t => t.Messages).Count()
                    })
                    .ToListAsync();

                return Ok(categories);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("forums")]
        public async Task<IActionResult> GetForums()
        {
            try
            {
                var forums = await _context.Forums
                    .Include(f => f.Category)
                    .Include(f => f.Threads)
                    .OrderBy(f => f.Category.SortOrder)
                    .ThenBy(f => f.Name) // Forum nie ma SortOrder
                    .Select(f => new
                    {
                        f.Id,
                        f.Name,
                        f.Description,
                        SortOrder = 0, // Forum nie ma SortOrder
                        IsActive = true, // Forum nie ma IsActive
                        f.CreatedAt,
                        Category = f.Category.Name,
                        ThreadCount = f.Threads.Count,
                        MessageCount = f.Threads.SelectMany(t => t.Messages).Count(),
                        LastActivity = f.Threads
                            .SelectMany(t => t.Messages)
                            .Max(m => (DateTime?)m.CreatedAt)
                    })
                    .ToListAsync();

                return Ok(forums);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("threads")]
        public async Task<IActionResult> GetThreads()
        {
            try
            {
                var threads = await _context.Threads
                    .Include(t => t.Author)
                    .Include(t => t.Forum)
                        .ThenInclude(f => f.Category)
                    .Include(t => t.Messages)
                    .OrderByDescending(t => t.CreatedAt)
                    .Select(t => new
                    {
                        t.Id,
                        t.Title,
                        t.CreatedAt,
                        IsLocked = false, // Thread nie ma IsLocked
                        t.IsPinned,
                        IsActive = true, // Thread nie ma IsActive
                        Author = t.Author.Username,
                        Forum = t.Forum.Name,
                        Category = t.Forum.Category.Name,
                        MessageCount = t.Messages.Count,
                        Views = t.Views,
                        LastActivity = t.Messages.Max(m => (DateTime?)m.CreatedAt) ?? t.CreatedAt
                    })
                    .ToListAsync();

                return Ok(threads);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("banned-words")]
        public async Task<IActionResult> GetBannedWords()
        {
            try
            {
                var bannedWords = await _context.BannedWords
                    .OrderBy(bw => bw.Word)
                    .Select(bw => new
                    {
                        bw.Id,
                        bw.Word,
                        Replacement = "***", // BannedWord nie ma Replacement
                        Severity = bw.SeverityLevel,
                        bw.IsActive,
                        bw.CreatedAt
                    })
                    .ToListAsync();

                return Ok(bannedWords);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("banned-words")]
        public async Task<IActionResult> AddBannedWord([FromBody] BannedWordRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Word))
                {
                    return BadRequest(new { error = "Słowo nie może być puste" });
                }

                // Sprawdź czy słowo już istnieje
                var existingWord = await _context.BannedWords
                    .FirstOrDefaultAsync(bw => bw.Word.ToLower() == request.Word.ToLower());

                if (existingWord != null)
                {
                    return BadRequest(new { error = "To słowo już jest na liście zakazanych" });
                }

                var bannedWord = new BannedWord
                {
                    Word = request.Word.Trim(),
                    SeverityLevel = request.Severity,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                _context.BannedWords.Add(bannedWord);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "Słowo zostało dodane do listy zakazanych",
                    bannedWord = new
                    {
                        bannedWord.Id,
                        bannedWord.Word,
                        Replacement = "***",
                        Severity = bannedWord.SeverityLevel,
                        bannedWord.IsActive,
                        bannedWord.CreatedAt
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpDelete("banned-words/{id}")]
        public async Task<IActionResult> RemoveBannedWord(int id)
        {
            try
            {
                var bannedWord = await _context.BannedWords.FindAsync(id);
                if (bannedWord == null)
                {
                    return NotFound(new { error = "Słowo nie zostało znalezione" });
                }

                _context.BannedWords.Remove(bannedWord);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Słowo zostało usunięte z listy zakazanych" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }

    public class BannedWordRequest
    {
        public string Word { get; set; } = string.Empty;
        public string? Replacement { get; set; }
        public SeverityLevel Severity { get; set; } = SeverityLevel.Warning;
    }
}
