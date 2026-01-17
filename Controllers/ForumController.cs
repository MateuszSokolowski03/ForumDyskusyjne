using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ForumDyskusyjne.Data;
using ForumDyskusyjne.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims; 
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
         private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdClaim, out var userId))
            {
                return userId;
            }
            throw new UnauthorizedAccessException("Użytkownik nie jest zalogowany");
        }

        // Metoda do sprawdzania zakazanych słów
        private async Task<(bool containsBannedWords, List<string> bannedWordsFound)> CheckBannedWords(string title, string content)
        {
            var bannedWordsList = await _context.BannedWords
                .Where(bw => bw.IsActive)
                .ToListAsync();

            var foundWords = new List<string>();
            var textToCheck = (title + " " + content).ToLower();

            foreach (var bannedWord in bannedWordsList)
            {
                var wordToCheck = bannedWord.Word.ToLower();
                
                if (bannedWord.MatchType == Models.MatchType.Exact)
                {
                    // Dokładne dopasowanie - całe słowo
                    if (System.Text.RegularExpressions.Regex.IsMatch(textToCheck, $@"\b{System.Text.RegularExpressions.Regex.Escape(wordToCheck)}\b"))
                    {
                        if (!foundWords.Contains(bannedWord.Word))
                            foundWords.Add(bannedWord.Word);
                    }
                }
                else if (bannedWord.MatchType == Models.MatchType.Contains)
                {
                    // Zawieranie - może być część słowa
                    if (textToCheck.Contains(wordToCheck))
                    {
                        if (!foundWords.Contains(bannedWord.Word))
                            foundWords.Add(bannedWord.Word);
                    }
                }
            }

            return (foundWords.Count > 0, foundWords);
        }

        [AllowAnonymous]
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

                if (categories == null || categories.Count == 0)
                {
                    return Ok(new List<object>()); // Zwróć pustą listę zamiast null
                }

                return Ok(categories);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Błąd GetCategories: {ex.Message}");
                return StatusCode(500, new { error = ex.Message, stackTrace = ex.StackTrace });
            }
        }
        [AllowAnonymous]
        [HttpGet("forum/{forumId}")]
        public async Task<IActionResult> GetForum(int forumId, int page = 1, int pageSize = 20, string? search = null)
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

                var threadsQuery = _context.Threads
                    .Where(t => t.ForumId == forumId);

                if (!string.IsNullOrWhiteSpace(search))
                {
                    threadsQuery = threadsQuery.Where(t => 
                        t.Title.Contains(search) || 
                        t.Messages.Any(m => m.Content.Contains(search))
                    );
                }

                var totalThreads = await threadsQuery.CountAsync();

                var threads = await threadsQuery
                    .Include(t => t.Author)
                    .Include(t => t.Messages.OrderByDescending(m => m.CreatedAt).Take(1))
                        .ThenInclude(m => m.Author)
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
        [AllowAnonymous]
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
         [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetUserThreads(int userId, int page = 1, int pageSize = 10)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    return NotFound(new { error = "Użytkownik nie został znaleziony" });
                }

                var totalThreads = await _context.Threads
                    .Where(t => t.AuthorId == userId)
                    .CountAsync();

                var threads = await _context.Threads
                    .Include(t => t.Author)
                    .Include(t => t.Forum)
                    .Include(t => t.Messages)
                    .Where(t => t.AuthorId == userId)
                    .OrderByDescending(t => t.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(t => new
                    {
                        t.Id,
                        t.Title,
                        t.CreatedAt,
                        t.Views,
                        RepliesCount = t.Messages.Count - 1,
                        Forum = new
                        {
                            t.Forum.Id,
                            t.Forum.Name
                        }
                    })
                    .ToListAsync();

                return Ok(new
                {
                    threads,
                    totalThreads,
                    totalPages = (int)Math.Ceiling((double)totalThreads / pageSize),
                    currentPage = page,
                    pageSize
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
        
       
        [Authorize]
        [HttpPost("thread")]
        public async Task<IActionResult> CreateThread([FromBody] CreateThreadRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Content))
                {
                    return BadRequest(new { error = "Tytuł i treść wątku są wymagane" });
                }

                int userId;
                try
                {
                    userId = GetCurrentUserId();
                }
                catch
                {
                    if (Request.Cookies.TryGetValue("user_session", out var sessionValue) && !string.IsNullOrEmpty(sessionValue))
                    {
                        var parts = sessionValue.Split('|');
                        if (parts.Length >= 1 && int.TryParse(parts[0], out var parsedId))
                        {
                            userId = parsedId;
                        }
                        else
                        {
                            return Unauthorized(new { error = "Brak sesji użytkownika" });
                        }
                    }
                    else
                    {
                        return Unauthorized(new { error = "Brak sesji użytkownika" });
                    }
                }

                var forum = await _context.Forums.FindAsync(request.ForumId);
                if (forum == null)
                {
                    return NotFound(new { error = "Forum nie znalezione (ID: " + request.ForumId + ")" });
                }

                var author = await _context.Users.FindAsync(userId);
                if (author == null)
                {
                    return Unauthorized(new { error = "Użytkownik nie istnieje" });
                }

                // Sprawdzenie zakazanych słów
                var (containsBannedWords, bannedWordsFound) = await CheckBannedWords(request.Title, request.Content);
                if (containsBannedWords)
                {
                    return BadRequest(new { 
                        error = "Wątek zawiera zakazane słowa i nie może być opublikowany", 
                        bannedWords = bannedWordsFound 
                    });
                }

                var thread = new Models.Thread
                {
                    Title = request.Title,
                    ForumId = request.ForumId,
                    AuthorId = userId,
                    CreatedAt = DateTime.UtcNow,
                    IsPinned = false,
                    Views = 0
                };

                _context.Threads.Add(thread);
                await _context.SaveChangesAsync();

                var message = new Message
                {
                    Content = request.Content,
                    ThreadId = thread.Id,
                    AuthorId = userId,
                    CreatedAt = DateTime.UtcNow,
                    IsEdited = false
                };

                _context.Messages.Add(message);
                await _context.SaveChangesAsync();

                author.PostCount++;
                _context.Users.Update(author);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, threadId = thread.Id, message = "Wątek utworzony" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Błąd: {ex.Message}\n{ex.StackTrace}");
                return StatusCode(500, new { error = ex.Message });
            }
        }


        [Authorize]
        [HttpPost("message")]
        public async Task<IActionResult> CreateMessage([FromBody] CreateMessageRequest request)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
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
                var author = await _context.Users.FindAsync(currentUserId);
                if (author == null)
                {
                    return NotFound(new { error = "Użytkownik nie został znaleziony" });
                }

                // Sprawdzenie zakazanych słów w treści wiadomości
                var (containsBannedWords, bannedWordsFound) = await CheckBannedWords("", request.Content);
                if (containsBannedWords)
                {
                    return BadRequest(new { 
                        error = "Wiadomość zawiera zakazane słowa i nie może być opublikowana", 
                        bannedWords = bannedWordsFound 
                    });
                }

                var message = new Message
                {
                    Content = request.Content.Trim(),
                    AuthorId = currentUserId,
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
        [AllowAnonymous]
        [HttpGet("categories/{categoryId}/threads")]
        public async Task<IActionResult> GetCategoryThreads(int categoryId, int page = 1, int pageSize = 20)
        {
            try
            {
                var category = await _context.Categories.FindAsync(categoryId);
                if (category == null)
                {
                    return NotFound(new { error = "Kategoria nie została znaleziona" });
                }

                var forums = await _context.Forums
                    .Where(f => f.CategoryId == categoryId)
                    .Select(f => f.Id)
                    .ToListAsync();

                var totalThreads = await _context.Threads
                    .Where(t => forums.Contains(t.ForumId))
                    .CountAsync();

                var threads = await _context.Threads
                    .Include(t => t.Author)
                    .Include(t => t.Forum)
                    .Include(t => t.Messages.OrderByDescending(m => m.CreatedAt).Take(1))
                        .ThenInclude(m => m.Author)
                    .Where(t => forums.Contains(t.ForumId))
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
                        RepliesCount = t.Messages.Count - 1,
                        Author = new
                        {
                            t.Author.Id,
                            t.Author.Username
                        },
                        Forum = new
                        {
                            t.Forum.Id,
                            t.Forum.Name
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
                    Category = new
                    {
                        category.Id,
                        category.Name,
                        category.Description
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

        [HttpPut("messages/{id}")]
        [Authorize]
public async Task<IActionResult> UpdateMessage(int id, [FromBody] UpdateMessageRequest request)
{
    try
    {
        if (string.IsNullOrWhiteSpace(request.Content))
        {
            return BadRequest(new { error = "Treść wiadomości jest wymagana" });
        }

        var message = await _context.Messages
            .Include(m => m.Thread)
            .FirstOrDefaultAsync(m => m.Id == id);
        
        if (message == null)
        {
            return NotFound(new { error = "Wiadomość nie została znaleziona" });
        }

        int currentUserId;
        try
        {
            currentUserId = GetCurrentUserId();
        }
        catch
        {
            return Unauthorized(new { error = "Brak sesji użytkownika" });
        }

        var currentUser = await _context.Users.FindAsync(currentUserId);
        if (currentUser == null)
            return Unauthorized(new { error = "Użytkownik nie został znaleziony" });

        // --- POPRAWIONA LOGIKA UPRAWNIEŃ ---
        bool isAllowed = false;

        // 1. Autor wiadomości ZAWSZE może edytować
        if (message.AuthorId == currentUserId)
        {
            isAllowed = true;
            System.Diagnostics.Debug.WriteLine($"✅ [EDIT] User {currentUserId} is Author.");
        }
        // 2. Admin ZAWSZE może edytować
        else if (currentUser.Role == UserRole.Admin)
        {
            isAllowed = true;
            System.Diagnostics.Debug.WriteLine($"✅ [EDIT] User {currentUserId} is Admin.");
        }
        // 3. Moderator może edytować, jeśli jest przypisany do tego forum
        else if (currentUser.Role == UserRole.Moderator)
        {
            var isModeratorForForum = await _context.ForumModerators
                .AnyAsync(fm => fm.UserId == currentUserId && fm.ForumId == message.Thread.ForumId);
            
            if (isModeratorForForum)
            {
                isAllowed = true;
                System.Diagnostics.Debug.WriteLine($"✅ [EDIT] User {currentUserId} is Moderator for this forum.");
            }
        }

        if (!isAllowed)
        {
            System.Diagnostics.Debug.WriteLine($"❌ [EDIT] User {currentUserId} (Role: {currentUser.Role}) forbidden to edit message {id}.");
            return Forbid();
        }
        // ------------------------------------

        // Sprawdzenie zakazanych słów
        var (containsBannedWords, bannedWordsFound) = await CheckBannedWords("", request.Content);
        if (containsBannedWords)
        {
             return BadRequest(new { 
                error = "Edytowana treść zawiera zakazane słowa", 
                bannedWords = bannedWordsFound 
            });
        }

        // Zapisz zmiany
        message.Content = request.Content.Trim();
        message.IsEdited = true;
        message.EditedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(new 
        { 
            message = "Wiadomość została edytowana",
            data = new
            {
                id = message.Id,
                content = message.Content,
                editedAt = message.EditedAt
            }
        });
    }
    catch (Exception ex)
    {
        return StatusCode(500, new { error = ex.Message });
    }
}

        [HttpDelete("messages/{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteMessage(int id, int userId)
        {
            try
            {
                var message = await _context.Messages
                    .Include(m => m.Thread)
                    .FirstOrDefaultAsync(m => m.Id == id);

                if (message == null)
                {
                    return NotFound(new { error = "Wiadomość nie została znaleziona" });
                }

                // Check if this is the first message of the thread
                var firstMessageInThread = await _context.Messages
                    .Where(m => m.ThreadId == message.ThreadId)
                    .OrderBy(m => m.CreatedAt)
                    .FirstOrDefaultAsync();

                if (firstMessageInThread != null && firstMessageInThread.Id == message.Id)
                {
                    // If it's the first message, delete the entire thread
                    System.Diagnostics.Debug.WriteLine($"🗑️ [DELETE] Message {id} is the first message in thread {message.ThreadId}. Deleting entire thread.");
                    var threadDeletionResult = await DeleteThreadInternal(message.ThreadId) as OkObjectResult;
                    if (threadDeletionResult != null)
                    {
                        var responseValue = threadDeletionResult.Value as dynamic;
                        return Ok(new { message = responseValue.message, isThreadDeleted = true });
                    }
                    return StatusCode(500, new { error = "Failed to delete thread after deleting first message." });
                }

                // Sprawdź, kto wykonuje akcję
                int currentUserId;
                try
                {
                    currentUserId = GetCurrentUserId();
                }
                catch
                {
                    return Unauthorized(new { error = "Brak sesji użytkownika" });
                }

                System.Diagnostics.Debug.WriteLine($"🗑️ [DELETE] Attempting to delete message {id}, author: {message.AuthorId}, requester: {currentUserId}, forum: {message.Thread.ForumId}");


                var currentUser = await _context.Users.FindAsync(currentUserId);
                if (currentUser == null)
                    return Unauthorized(new { error = "Użytkownik nie został znaleziony" });

                System.Diagnostics.Debug.WriteLine($"🗑️ [DELETE] Current user role: {currentUser.Role}");

                // Autor wiadomości może usunąć swoją wiadomość
                if (message.AuthorId == currentUserId)
                {
                    System.Diagnostics.Debug.WriteLine($"✅ [DELETE] Author is deleting own message");
                }
                // Moderator przypisany do forum może usunąć wiadomość
                else if (currentUser.Role == UserRole.Moderator)
                {
                    var isModeratorForForum = await _context.ForumModerators
                        .AnyAsync(fm => fm.UserId == currentUserId && fm.ForumId == message.Thread.ForumId);

                    System.Diagnostics.Debug.WriteLine($"🔍 [DELETE] Moderator {currentUserId} forum check: isModeratorForForum={isModeratorForForum}, requiredForumId={message.Thread.ForumId}");

                    if (!isModeratorForForum)
                    {
                        System.Diagnostics.Debug.WriteLine($"❌ [DELETE] Moderator not assigned to forum {message.Thread.ForumId}");
                        return Forbid();
                    }
                    System.Diagnostics.Debug.WriteLine($"✅ [DELETE] Moderator assigned to forum");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"❌ [DELETE] User {currentUserId} is not authorized to delete this message");
                    return Forbid();
                }

                _context.Messages.Remove(message);
                
                // Aktualizuj licznik odpowiedzi w wątku
                if (message.Thread.RepliesCount > 0)
                {
                    message.Thread.RepliesCount--;
                }

                await _context.SaveChangesAsync();

                System.Diagnostics.Debug.WriteLine($"✅ [DELETE] Message {id} deleted successfully");
                return Ok(new { message = "Wiadomość została usunięta", isThreadDeleted = false });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ [DELETE] Error: {ex.Message}");

                return StatusCode(500, new { error = ex.Message });
            }
        }
        [HttpDelete("thread/{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteThread(int id)
        {
            return await DeleteThreadInternal(id);
        }

        private async Task<IActionResult> DeleteThreadInternal(int id)
{
    try
    {
        var thread = await _context.Threads
            .Include(t => t.Forum)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (thread == null)
        {
            return NotFound(new { error = "Wątek nie został znaleziony" });
        }

        int currentUserId;
        try
        {
            currentUserId = GetCurrentUserId();
        }
        catch
        {
            return Unauthorized(new { error = "Użytkownik nie jest zalogowany" });
        }

        var currentUser = await _context.Users.FindAsync(currentUserId);
        if (currentUser == null)
            return Unauthorized(new { error = "Użytkownik nie został znaleziony" });

        // --- SPRAWDZANIE UPRAWNIEŃ ---
        bool isAuthorized = false;

        // 1. Autor wątku
        if (thread.AuthorId == currentUserId)
        {
            isAuthorized = true;
        }
        // 2. Administrator
        else if (currentUser.Role == UserRole.Admin)
        {
            isAuthorized = true;
        }
        // 3. Moderator forum
        else if (currentUser.Role == UserRole.Moderator)
        {
            isAuthorized = await _context.ForumModerators
                .AnyAsync(fm => fm.UserId == currentUserId && fm.ForumId == thread.ForumId);
        }

        if (!isAuthorized)
        {
            return Forbid();
        }

        // --- USUWANIE DANYCH ---
        
        // 1. Pobierz wiadomości WRAZ z załącznikami
        var messages = await _context.Messages
            .Include(m => m.Attachments) // Kluczowe dla uniknięcia błędu FK
            .Where(m => m.ThreadId == id)
            .ToListAsync();

        // 2. Usuń załączniki (jeśli istnieją)
        foreach (var msg in messages)
        {
            if (msg.Attachments != null && msg.Attachments.Any())
            {
                _context.RemoveRange(msg.Attachments);
            }
        }

        // 3. Usuń wiadomości i wątek
        _context.Messages.RemoveRange(messages);
        _context.Threads.Remove(thread);

        await _context.SaveChangesAsync();

        return Ok(new { message = "Wątek i powiązane wiadomości zostały usunięte" });
    }
    catch (Exception ex)
    {
        // Zwróć dokładniejszy błąd (np. błąd SQL)
        var msg = ex.Message;
        if (ex.InnerException != null) msg += " | Inner: " + ex.InnerException.Message;
        return StatusCode(500, new { error = msg });
    }
}
    }

    public class CreateThreadRequest
    {
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public int ForumId { get; set; }
        
    }

    public class CreateMessageRequest
    {
        public string Content { get; set; } = string.Empty;
        public int ThreadId { get; set; }
         // Tymczasowo, później z sesji/tokena
    }

    public class UpdateMessageRequest
    {
        public string Content { get; set; } = string.Empty;
    }
}
