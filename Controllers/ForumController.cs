using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ForumDyskusyjne.Data;
using ForumDyskusyjne.Models;

namespace ForumDyskusyjne.Controllers
{
    [Route("api/forum")]
    [ApiController]
    public class ForumController : ControllerBase
    {
        private readonly ForumDbContext _context;

        public ForumController(ForumDbContext context)
        {
            _context = context;
        }

        [HttpGet("categories")]
        public async Task<IActionResult> GetCategories()
        {
            try
            {
                var categories = await _context.Categories
                    .Include(c => c.Forums)
                        .ThenInclude(f => f.Threads)
                    .OrderBy(c => c.SortOrder)
                    .Select(c => new
                    {
                        c.Id,
                        c.Name,
                        c.Description,
                        c.Icon,
                        c.SortOrder,
                        Forums = c.Forums.Select(f => new
                        {
                            f.Id,
                            f.Name,
                            f.Description,
                            ThreadCount = f.Threads.Count,
                            MessageCount = f.Threads.SelectMany(t => t.Messages).Count(),
                            LastActivity = f.Threads
                                .SelectMany(t => t.Messages)
                                .OrderByDescending(m => m.CreatedAt)
                                .Select(m => new
                                {
                                    m.CreatedAt,
                                    AuthorName = m.Author.Username,
                                    ThreadTitle = m.Thread.Title,
                                    ThreadId = m.Thread.Id
                                })
                                .FirstOrDefault()
                        }).ToList()
                    })
                    .ToListAsync();

                return Ok(categories);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("forum/{forumId}")]
        public async Task<IActionResult> GetForum(int forumId, int page = 1, int pageSize = 20)
        {
            try
            {
                var forum = await _context.Forums
                    .Include(f => f.Category)
                    .FirstOrDefaultAsync(f => f.Id == forumId);

                if (forum == null)
                {
                    return NotFound(new { error = "Forum nie zostało znalezione" });
                }

                var totalThreads = await _context.Threads
                    .Where(t => t.ForumId == forumId)
                    .CountAsync();

                var threads = await _context.Threads
                    .Include(t => t.Author)
                    .Include(t => t.Messages.OrderByDescending(m => m.CreatedAt).Take(1))
                        .ThenInclude(m => m.Author)
                    .Where(t => t.ForumId == forumId)
                    .OrderByDescending(t => t.IsPinned)
                    .ThenByDescending(t => t.Messages.Max(m => (DateTime?)m.CreatedAt) ?? t.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(t => new
                    {
                        t.Id,
                        t.Title,
                        t.CreatedAt,
                        t.IsPinned,
                        t.Views,
                        RepliesCount = t.Messages.Count - 1, // -1 bo pierwszy post nie jest odpowiedzią
                        Author = new
                        {
                            t.Author.Id,
                            t.Author.Username
                        },
                        LastMessage = t.Messages.OrderByDescending(m => m.CreatedAt).Select(m => new
                        {
                            m.CreatedAt,
                            Author = new
                            {
                                m.Author.Id,
                                m.Author.Username
                            }
                        }).FirstOrDefault()
                    })
                    .ToListAsync();

                return Ok(new
                {
                    Forum = new
                    {
                        forum.Id,
                        forum.Name,
                        forum.Description,
                        Category = forum.Category.Name
                    },
                    Threads = threads,
                    TotalThreads = totalThreads,
                    TotalPages = (int)Math.Ceiling((double)totalThreads / pageSize),
                    CurrentPage = page,
                    PageSize = pageSize
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("thread/{threadId}")]
        public async Task<IActionResult> GetThread(int threadId, int page = 1, int pageSize = 20)
        {
            try
            {
                var thread = await _context.Threads
                    .Include(t => t.Author)
                    .Include(t => t.Forum)
                        .ThenInclude(f => f.Category)
                    .FirstOrDefaultAsync(t => t.Id == threadId);

                if (thread == null)
                {
                    return NotFound(new { error = "Wątek nie został znaleziony" });
                }

                // Zwiększ liczbę wyświetleń
                thread.Views++;
                await _context.SaveChangesAsync();

                var totalMessages = await _context.Messages
                    .Where(m => m.ThreadId == threadId)
                    .CountAsync();

                var messages = await _context.Messages
                    .Include(m => m.Author)
                        .ThenInclude(a => a.CurrentRank)
                    .Include(m => m.Attachments)
                    .Where(m => m.ThreadId == threadId)
                    .OrderBy(m => m.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(m => new
                    {
                        m.Id,
                        m.Content,
                        m.CreatedAt,
                        UpdatedAt = m.EditedAt,
                        Author = new
                        {
                            m.Author.Id,
                            m.Author.Username,
                            m.Author.AvatarUrl,
                            Rank = m.Author.CurrentRank != null ? m.Author.CurrentRank.Name : "Użytkownik",
                            PostCount = m.Author.PostCount,
                            JoinDate = m.Author.CreatedAt
                        },
                        Attachments = m.Attachments.Select(a => new
                        {
                            a.Id,
                            FileName = a.OriginalFilename,
                            a.FileSize,
                            FileType = a.MimeType
                        }).ToList()
                    })
                    .ToListAsync();

                return Ok(new
                {
                    Thread = new
                    {
                        thread.Id,
                        thread.Title,
                        thread.CreatedAt,
                        thread.IsPinned,
                        thread.Views,
                        Author = new
                        {
                            thread.Author.Id,
                            thread.Author.Username
                        },
                        Forum = new
                        {
                            thread.Forum.Id,
                            thread.Forum.Name,
                            Category = thread.Forum.Category.Name
                        }
                    },
                    Messages = messages,
                    TotalMessages = totalMessages,
                    TotalPages = (int)Math.Ceiling((double)totalMessages / pageSize),
                    CurrentPage = page,
                    PageSize = pageSize
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("thread")]
        public async Task<IActionResult> CreateThread([FromBody] CreateThreadRequest request)
        {
            try
            {
                // TODO: Walidacja użytkownika (po implementacji autentyfikacji)
                
                if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Content))
                {
                    return BadRequest(new { error = "Tytuł i treść są wymagane" });
                }

                var forum = await _context.Forums.FindAsync(request.ForumId);
                if (forum == null)
                {
                    return NotFound(new { error = "Forum nie zostało znalezione" });
                }

                // Sprawdź czy użytkownik istnieje (tymczasowo)
                var author = await _context.Users.FindAsync(request.AuthorId);
                if (author == null)
                {
                    return NotFound(new { error = "Użytkownik nie został znaleziony" });
                }

                var thread = new Models.Thread
                {
                    Title = request.Title.Trim(),
                    AuthorId = request.AuthorId,
                    ForumId = request.ForumId,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Threads.Add(thread);
                await _context.SaveChangesAsync();

                // Dodaj pierwszy post
                var firstMessage = new Message
                {
                    Content = request.Content.Trim(),
                    AuthorId = request.AuthorId,
                    ThreadId = thread.Id,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Messages.Add(firstMessage);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "Wątek został utworzony",
                    threadId = thread.Id
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("message")]
        public async Task<IActionResult> CreateMessage([FromBody] CreateMessageRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Content))
                {
                    return BadRequest(new { error = "Treść wiadomości jest wymagana" });
                }

                var thread = await _context.Threads.FindAsync(request.ThreadId);
                if (thread == null)
                {
                    return NotFound(new { error = "Wątek nie został znaleziony" });
                }

                // Sprawdź czy użytkownik istnieje (tymczasowo)
                var author = await _context.Users.FindAsync(request.AuthorId);
                if (author == null)
                {
                    return NotFound(new { error = "Użytkownik nie został znaleziony" });
                }

                var message = new Message
                {
                    Content = request.Content.Trim(),
                    AuthorId = request.AuthorId,
                    ThreadId = request.ThreadId,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Messages.Add(message);
                
                // Aktualizuj licznik odpowiedzi w wątku
                thread.RepliesCount++;
                
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "Odpowiedź została dodana",
                    messageId = message.Id
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }

    public class CreateThreadRequest
    {
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public int ForumId { get; set; }
        public int AuthorId { get; set; } // Tymczasowo, później z sesji/tokena
    }

    public class CreateMessageRequest
    {
        public string Content { get; set; } = string.Empty;
        public int ThreadId { get; set; }
        public int AuthorId { get; set; } // Tymczasowo, później z sesji/tokena
    }
}
