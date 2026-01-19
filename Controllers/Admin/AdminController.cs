using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ForumDyskusyjne.Data;
using ForumDyskusyjne.Models;
using Microsoft.AspNetCore.Authorization;

namespace ForumDyskusyjne.Controllers
{
        [Route("api/admin")]
        [ApiController]
        [Authorize(Roles = "Admin")]
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
                        {
                            u.Id,
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
                        AvatarUrl = u.AvatarUrl,
                        created_at = u.CreatedAt,
                        last_activity_at = u.LastActivityAt,
                        role = u.Role.ToString(),
                        status = u.IsBanned ? "blocked" : (u.EmailVerified ? "active" : "pending"),
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
                        IsActive = true,
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
        public async Task<IActionResult> GetForums(int? categoryId = null)
        {
            try
            {
                var query = _context.Forums
                    .Include(f => f.Category)
                    .AsQueryable();

                if (categoryId.HasValue)
                {
                    query = query.Where(f => f.CategoryId == categoryId.Value);
                }

                var forums = await query
                    .OrderBy(f => f.Category.SortOrder)
                    .ThenBy(f => f.Name)
                    .Select(f => new
                    {
                        f.Id,
                        f.Name,
                        f.Description,
                        f.CategoryId,
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

        [HttpPost("forums")]
        public async Task<IActionResult> CreateForum([FromBody] CreateForumRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Name))
                    return BadRequest(new { error = "Nazwa forum jest wymagana" });

                if (request.CategoryId <= 0)
                    return BadRequest(new { error = "Kategoria jest wymagana" });

                var category = await _context.Categories.FindAsync(request.CategoryId);
                if (category == null)
                    return NotFound(new { error = "Kategoria nie znaleziona" });

                var forum = new Forum
                {
                    Name = request.Name.Trim(),
                    Description = request.Description?.Trim(),
                    CategoryId = request.CategoryId,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Forums.Add(forum);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetForums), new { categoryId = forum.CategoryId }, new
                {
                    forum.Id,
                    forum.Name,
                    forum.Description,
                    forum.CategoryId,
                    message = "Forum zostało utworzone"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPut("forums/{id}")]
        public async Task<IActionResult> UpdateForum(int id, [FromBody] UpdateForumRequest request)
        {
            try
            {
                var forum = await _context.Forums.FindAsync(id);
                if (forum == null)
                    return NotFound(new { error = "Forum nie znalezione" });

                if (!string.IsNullOrWhiteSpace(request.Name))
                    forum.Name = request.Name.Trim();

                if (request.Description != null)
                    forum.Description = request.Description.Trim();

                _context.Forums.Update(forum);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    forum.Id,
                    forum.Name,
                    forum.Description,
                    message = "Forum zostało zaktualizowane"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpDelete("forums/{id}")]
        public async Task<IActionResult> DeleteForum(int id)
        {
            try
            {
                var forum = await _context.Forums.FindAsync(id);
                if (forum == null)
                    return NotFound(new { error = "Forum nie znalezione" });

                _context.Forums.Remove(forum);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Forum zostało usunięte" });
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
                    .OrderByDescending(t => t.CreatedAt)
                    .Select(t => new
                    {
                        t.Id,
                        t.Title,
                        authorName = t.Author.Username,
                        forumName = t.Forum.Name,
                        t.RepliesCount,
                        t.Views,
                        t.CreatedAt
                    })
                    .ToListAsync();

                return Ok(threads);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPut("threads/{id}/block")]
        public async Task<IActionResult> BlockThread(int id)
        {
            try
            {
                var thread = await _context.Threads.FindAsync(id);
                if (thread == null)
                    return NotFound(new { error = "Wątek nie znaleziony" });

                return Ok(new { message = "Wątek zablokowany" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("threads/{id}/report")]
        public async Task<IActionResult> ReportThread(int id, [FromBody] ReportThreadRequest request)
        {
            try
            {
                var thread = await _context.Threads.FindAsync(id);
                if (thread == null)
                    return NotFound(new { error = "Wątek nie znaleziony" });

                return Ok(new { message = "Wątek zgłoszony" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpDelete("threads/{id}")]
        public async Task<IActionResult> DeleteThread(int id)
        {
            try
            {
                var thread = await _context.Threads.FindAsync(id);
                if (thread == null)
                    return NotFound(new { error = "Wątek nie znaleziony" });

                _context.Threads.Remove(thread);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Wątek usunięty" });
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
                        Replacement = "***",
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

        [HttpPost("categories")]
        public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Name))
                {
                    return BadRequest(new { error = "Nazwa kategorii jest wymagana" });
                }

                var existingCategory = await _context.Categories
                    .FirstOrDefaultAsync(c => c.Name.ToLower() == request.Name.ToLower());

                if (existingCategory != null)
                {
                    return BadRequest(new { error = "Kategoria o takiej nazwie już istnieje" });
                }

                var maxSortOrder = await _context.Categories.MaxAsync(c => (int?)c.SortOrder) ?? 0;

                var category = new Category
                {
                    Name = request.Name.Trim(),
                    Description = request.Description?.Trim(),
                    Icon = request.Icon?.Trim(),
                    SortOrder = maxSortOrder + 1,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Categories.Add(category);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "Kategoria została utworzona",
                    category = new
                    {
                        category.Id,
                        category.Name,
                        category.Description,
                        category.Icon,
                        category.SortOrder,
                        category.CreatedAt
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPut("categories/{id}")]
        public async Task<IActionResult> UpdateCategory(int id, [FromBody] UpdateCategoryRequest request)
        {
            try
            {
                var category = await _context.Categories.FindAsync(id);
                if (category == null)
                {
                    return NotFound(new { error = "Kategoria nie została znaleziona" });
                }

                if (!string.IsNullOrWhiteSpace(request.Name))
                {
                    var existingCategory = await _context.Categories
                        .FirstOrDefaultAsync(c => c.Name.ToLower() == request.Name.ToLower() && c.Id != id);

                    if (existingCategory != null)
                    {
                        return BadRequest(new { error = "Kategoria o takiej nazwie już istnieje" });
                    }

                    category.Name = request.Name.Trim();
                }

                if (request.Description != null)
                {
                    category.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
                }

                if (request.Icon != null)
                {
                    category.Icon = string.IsNullOrWhiteSpace(request.Icon) ? null : request.Icon.Trim();
                }

                await _context.SaveChangesAsync();

                return Ok(new { message = "Kategoria została zaktualizowana" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpDelete("categories/{id}")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            try
            {
                var category = await _context.Categories
                    .Include(c => c.Forums)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (category == null)
                {
                    return NotFound(new { error = "Kategoria nie została znaleziona" });
                }

                if (category.Forums.Any())
                {
                    return BadRequest(new { error = "Nie można usunąć kategorii która zawiera fora. Usuń najpierw wszystkie fora." });
                }

                _context.Categories.Remove(category);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Kategoria została usunięta" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPut("categories/reorder")]
        public async Task<IActionResult> ReorderCategories([FromBody] ReorderCategoriesRequest request)
        {
            try
            {
                if (request.CategoryOrders == null || !request.CategoryOrders.Any())
                {
                    return BadRequest(new { error = "Lista kategorii jest wymagana" });
                }

                foreach (var order in request.CategoryOrders)
                {
                    var category = await _context.Categories.FindAsync(order.CategoryId);
                    if (category != null)
                    {
                        category.SortOrder = order.SortOrder;
                    }
                }

                await _context.SaveChangesAsync();

                return Ok(new { message = "Kolejność kategorii została zaktualizowana" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPut("users/{id}/ban")]
        public async Task<IActionResult> BanUser(int id, [FromBody] BanUserRequest request)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                {
                    return NotFound(new { error = "Użytkownik nie został znaleziony" });
                }

                if (user.Role == UserRole.Admin)
                {
                    return BadRequest(new { error = "Nie można zbanować administratora" });
                }

                user.IsBanned = true;
                user.BanReason = request.Reason?.Trim();
                user.BanExpiresAt = request.ExpiresAt;

                await _context.SaveChangesAsync();

                return Ok(new { message = "Użytkownik został zbanowany" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("users/{id}/block")]
        public async Task<IActionResult> BlockUser(int id)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                {
                    return NotFound(new { error = "Użytkownik nie został znaleziony" });
                }

                if (user.Role == UserRole.Admin)
                {
                    return BadRequest(new { error = "Nie można zablokować administratora" });
                }

                user.IsBanned = true;
                await _context.SaveChangesAsync();

                return Ok(new { message = "Użytkownik został zablokowany" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPut("users/{id}/unban")]
        public async Task<IActionResult> UnbanUser(int id)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                {
                    return NotFound(new { error = "Użytkownik nie został znaleziony" });
                }

                user.IsBanned = false;
                user.BanReason = null;
                user.BanExpiresAt = null;

                await _context.SaveChangesAsync();

                return Ok(new { message = "Ban użytkownika został zniesiony" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("users/{id}/unblock")]
        public async Task<IActionResult> UnblockUser(int id)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                {
                    return NotFound(new { error = "Użytkownik nie został znaleziony" });
                }

                user.IsBanned = false;
                await _context.SaveChangesAsync();

                return Ok(new { message = "Użytkownik został odblokowany" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("users/{id}/verify-email")]
        public async Task<IActionResult> VerifyUserEmail(int id)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                {
                    return NotFound(new { error = "Użytkownik nie został znaleziony" });
                }

                user.EmailVerified = true;
                await _context.SaveChangesAsync();

                return Ok(new { message = "Email użytkownika został potwierdzony" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPut("users/{id}/role")]
        public async Task<IActionResult> ChangeUserRole(int id, [FromBody] ChangeRoleRequest request)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                {
                    return NotFound(new { error = "Użytkownik nie został znaleziony" });
                }

                // Konwertuj string na enum
                if (!Enum.TryParse<UserRole>(request.Role, out var role))
                {
                    return BadRequest(new { error = "Nieprawidłowa rola. Dozwolone wartości: User, Moderator, Admin" });
                }

                user.Role = role;
                await _context.SaveChangesAsync();

                return Ok(new { message = "Rola użytkownika została zmieniona" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

         [HttpGet("moderators")]
        public async Task<IActionResult> GetModerators()
        {
            try
            {
                // Get all moderators with their assigned forums
                var allModerators = await _context.Users
                    .Where(u => u.Role == UserRole.Moderator)
                    .Include(u => u.ModeratedForums)
                    .ThenInclude(fm => fm.Forum)
                    .ToListAsync();

                var result = allModerators.Select(u => new
                {
                    userId = u.Id,
                    username = u.Username,
                    email = u.Email,
                    forums = u.ModeratedForums.Select(fm => new
                    {
                        forumId = fm.ForumId,
                        forumName = fm.Forum.Name
                    }).ToList()
                }).ToList();

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

       [HttpPost("moderators")]
        public async Task<IActionResult> CreateModerator([FromBody] CreateModeratorRequest request)
        {
            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == request.Username);
                if (user == null)
                    return NotFound(new { error = "Użytkownik nie znaleziony" });

                int? forumId = null;

                // Jeśli podano nazwę forum
                if (!string.IsNullOrEmpty(request.ForumName))
                {
                    var forum = await _context.Forums
                        .FirstOrDefaultAsync(f => f.Name.ToLower() == request.ForumName.ToLower());
                    
                    if (forum == null)
                        return NotFound(new { error = "Forum nie znalezione" });

                    forumId = forum.Id;
                }
                // Jeśli podano ID forum
                else if (request.ForumId.HasValue)
                {
                    forumId = request.ForumId.Value;
                }

                // Forum is required when creating a moderator mapping
                if (!forumId.HasValue)
                {
                    return BadRequest(new { error = "Forum jest wymagane" });
                }

                // Check if the forum already has any moderator (other than this user)
                var occupied = await _context.ForumModerators
                    .AnyAsync(m => m.ForumId == forumId.Value && m.UserId != user.Id);

                if (occupied)
                {
                    return BadRequest(new { error = "To forum ma już przypisanego moderatora" });
                }

                // Check if this moderator already has this forum assigned
                var existingMapping = await _context.ForumModerators
                    .FirstOrDefaultAsync(m => m.UserId == user.Id && m.ForumId == forumId.Value);

                if (existingMapping != null)
                    return BadRequest(new { error = "Moderator ma już przypisane to forum" });

                var moderator = new ForumModerator
                {
                    UserId = user.Id,
                    ForumId = forumId.Value
                };

                _context.ForumModerators.Add(moderator);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetModerators), new { message = "Moderator dodany" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPut("moderators/{username}")]
        public async Task<IActionResult> UpdateModerator(string username, [FromBody] UpdateModeratorRequest request)
        {
            try
            {
                // Find the user first
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
                if (user == null)
                    return NotFound(new { error = "Moderator nie znaleziony" });

                int? newForumId = null;

                // If provided a forum name, resolve it
                if (!string.IsNullOrEmpty(request.ForumName))
                {
                    var newForum = await _context.Forums
                        .FirstOrDefaultAsync(f => f.Name.ToLower() == request.ForumName.ToLower());

                    if (newForum == null)
                        return NotFound(new { error = "Forum nie znalezione" });

                    newForumId = newForum.Id;
                }
                else if (request.ForumId.HasValue)
                {
                    newForumId = request.ForumId.Value;
                }

                if (!newForumId.HasValue)
                    return BadRequest(new { error = "Forum jest wymagane" });

                // Check if this moderator already has this forum assigned
                var existingMapping = await _context.ForumModerators
                    .FirstOrDefaultAsync(m => m.UserId == user.Id && m.ForumId == newForumId.Value);

                if (existingMapping != null)
                    return BadRequest(new { error = "Moderator ma już przypisane to forum" });

                // Ensure that the forum isn't assigned to another moderator
                var otherMapping = await _context.ForumModerators
                    .FirstOrDefaultAsync(m => m.ForumId == newForumId.Value && m.UserId != user.Id);

                if (otherMapping != null)
                    return BadRequest(new { error = "To forum ma już przypisanego innego moderatora" });

                // Add new forum assignment for this moderator
                var newMapping = new ForumModerator
                {
                    UserId = user.Id,
                    ForumId = newForumId.Value
                };

                _context.ForumModerators.Add(newMapping);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Forum przypisane moderatorowi" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
        [HttpDelete("moderators/{username}")]
        public async Task<IActionResult> DeleteModerator(string username)
        {
            try
            {
                var moderator = await _context.ForumModerators
                    .Include(m => m.User)
                    .FirstOrDefaultAsync(m => m.User.Username == username);
                    
                if (moderator == null)
                    return NotFound(new { error = "Moderator nie znaleziony" });

                _context.ForumModerators.Remove(moderator);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Moderator usunięty" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // Settings endpoints
        [HttpGet("settings/logo")]
        [AllowAnonymous]
        public async Task<IActionResult> GetLogoSettings()
        {
            try
            {
                var settings = await _context.ForumSettings.FirstOrDefaultAsync();
                if (settings == null)
                {
                    // Zwróć domyślne ustawienia
                    return Ok(new {
                        logoUrl = (string?)null,
                        forumName = "Forum Dyskusyjne"
                    });
                }

                return Ok(new {
                    logoUrl = settings.LogoUrl,
                    forumName = settings.ForumName
                });
            }
            catch (Exception ex)
            {
                // Log the exception server-side for diagnostics but return safe defaults to frontend
                try { Console.Error.WriteLine($"GetLogoSettings error: {ex}"); } catch {}
                return Ok(new {
                    logoUrl = (string?)null,
                    forumName = "Forum Dyskusyjne"
                });
            }
        }

        [HttpPost("settings/logo")]
        public async Task<IActionResult> UpdateLogoSettings([FromBody] UpdateLogoRequest request)
        {
            try
            {
                // Sprawdź czy użytkownik jest adminami
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int id))
                {
                    return Unauthorized(new { error = "Użytkownik nie zalogowany" });
                }

                var user = await _context.Users.FindAsync(id);
                if (user?.Role != UserRole.Admin)
                {
                    return Forbid();
                }

                var settings = await _context.ForumSettings.FirstOrDefaultAsync();
                if (settings == null)
                {
                    settings = new ForumSettings 
                    { 
                        ForumName = request.ForumName ?? "Forum Dyskusyjne",
                        LogoUrl = request.LogoUrl,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _context.ForumSettings.Add(settings);
                }
                else
                {
                    if (!string.IsNullOrEmpty(request.ForumName))
                        settings.ForumName = request.ForumName;
                    if (request.LogoUrl != null)
                        settings.LogoUrl = request.LogoUrl;
                    settings.UpdatedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();
                
                return Ok(new { 
                    message = "Ustawienia loga zostały zaktualizowane",
                    logoUrl = settings.LogoUrl,
                    forumName = settings.ForumName
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }

    // === REQUEST DTOs ===

    public class UpdateLogoRequest
    {
        public string? LogoUrl { get; set; }
        public string? ForumName { get; set; }
    }    public class BannedWordRequest
    {
        public string Word { get; set; } = string.Empty;
        public string? Replacement { get; set; }
        public SeverityLevel Severity { get; set; } = SeverityLevel.Warning;
    }

    public class CreateForumRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int CategoryId { get; set; }
    }

    public class UpdateForumRequest
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
    }

    public class CreateCategoryRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Icon { get; set; }
    }

    public class UpdateCategoryRequest
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? Icon { get; set; }
    }

    public class ReorderCategoriesRequest
    {
        public List<CategoryOrder> CategoryOrders { get; set; } = new();
    }

    public class CategoryOrder
    {
        public int CategoryId { get; set; }
        public int SortOrder { get; set; }
    }

    public class BanUserRequest
    {
        public string? Reason { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }

    public class ChangeRoleRequest
    {
        public string Role { get; set; } = string.Empty;
    }

    public class ReportThreadRequest
    {
        public string Reason { get; set; } = string.Empty;
    }

    public class CreateModeratorRequest
    {
       public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int? ForumId { get; set; }
        public string? ForumName { get; set; }
    }

    public class UpdateModeratorRequest
    {
        public int? ForumId { get; set; }
        public string? ForumName { get; set; }
    }
}