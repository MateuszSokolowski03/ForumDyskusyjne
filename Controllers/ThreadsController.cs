using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization; // Add this line
using ForumDyskusyjne.Data;
using ForumDyskusyjne.Models;

namespace ForumDyskusyjne
{
    public class ThreadsController : Controller
    {
        private readonly ForumDbContext _context;

        public ThreadsController(ForumDbContext context)
        {
            _context = context;
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

        // GET: Threads
        public async Task<IActionResult> Index()
        {
            var forumDbContext = _context.Threads.Include(t => t.Author).Include(t => t.Forum);
            return View(await forumDbContext.ToListAsync());
        }

        // GET: Threads/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var thread = await _context.Threads
                .Include(t => t.Author)
                .Include(t => t.Forum)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (thread == null)
            {
                return NotFound();
            }

            return View(thread);
        }

        // GET: Threads/Create
        public IActionResult Create()
        {
            ViewData["AuthorId"] = new SelectList(_context.Users, "Id", "Email");
            ViewData["ForumId"] = new SelectList(_context.Forums, "Id", "Name");
            return View();
        }

        // POST: Threads/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        // ...existing code...


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Title,AuthorId,ForumId,IsPinned,Views,RepliesCount,CreatedAt")] Models.Thread thread, string InitialMessage)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(thread.Title))
                {
                    ModelState.AddModelError(nameof(thread.Title), "Tytuł wątku jest wymagany");
                }

                // Sprawdzenie zakazanych słów w tytule i treści
                var (containsBannedWords, bannedWordsFound) = await CheckBannedWords(thread.Title, InitialMessage ?? "");
                if (containsBannedWords)
                {
                    ModelState.AddModelError("", $"Wątek zawiera zakazane słowa: {string.Join(", ", bannedWordsFound)}");
                }

                // jeśli AuthorId nie został wypełniony (formularz publiczny), spróbuj pobrać z Claims
                if (thread.AuthorId == 0 && User?.Identity?.IsAuthenticated == true)
                {
                    var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    if (int.TryParse(idClaim, out var uid))
                    {
                        thread.AuthorId = uid;
                    }
                }

                if (!ModelState.IsValid)
                {
                    ViewData["AuthorId"] = new SelectList(_context.Users, "Id", "Email", thread.AuthorId);
                    ViewData["ForumId"] = new SelectList(_context.Forums, "Id", "Name", thread.ForumId);
                    return View(thread);
                }

                thread.CreatedAt = DateTime.UtcNow;
                _context.Add(thread);
                await _context.SaveChangesAsync(); // zapisujemy żeby mieć thread.Id

                // jeśli podano treść pierwszego posta - utwórz message
                if (!string.IsNullOrWhiteSpace(InitialMessage))
                {
                    var message = new Message
                    {
                        ThreadId = thread.Id,
                        AuthorId = thread.AuthorId,
                        Content = InitialMessage,
                        CreatedAt = DateTime.UtcNow,
                        IsEdited = false
                    };
                    _context.Messages.Add(message);

                    // opcjonalnie zaktualizuj licznik odpowiedzi/posta w wątku/użytkowniku
                    thread.RepliesCount = 0;
                    await _context.SaveChangesAsync();
                }

                return RedirectToAction("Details", new { id = thread.Id });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return StatusCode(500, ex.Message);
            }
        }


        // POST: Threads/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,AuthorId,ForumId,IsPinned,Views,RepliesCount,CreatedAt")] Models.Thread thread)
        {
            if (id != thread.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(thread);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ThreadExists(thread.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["AuthorId"] = new SelectList(_context.Users, "Id", "Email", thread.AuthorId);
            ViewData["ForumId"] = new SelectList(_context.Forums, "Id", "Name", thread.ForumId);
            return View(thread);
        }

        // GET: Threads/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var thread = await _context.Threads
                .Include(t => t.Author)
                .Include(t => t.Forum)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (thread == null)
            {
                return NotFound();
            }

            return View(thread);
        }

        // POST: Threads/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var thread = await _context.Threads.FindAsync(id);
            if (thread != null)
            {
                _context.Threads.Remove(thread);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // DELETE: api/forum/thread/{id}
        [HttpDelete]
        [Route("/api/forum/thread/{id}")]
        [Authorize] // Only authenticated users can delete threads
        public async Task<IActionResult> DeleteThreadApi(int id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var currentUserId))
            {
                return StatusCode(401, new { error = "Użytkownik nie jest zalogowany." }); // Unauthorized
            }

            var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;

            var thread = await _context.Threads
                                     .Include(t => t.Messages) // Include messages to check for existence
                                     .FirstOrDefaultAsync(t => t.Id == id);

            if (thread == null)
            {
                return StatusCode(404, new { error = "Wątek nie został znaleziony." }); // Not Found
            }

            // Authorization check
            bool isAuthor = thread.AuthorId == currentUserId;
            bool isAdmin = currentUserRole == UserRole.Admin.ToString();
            bool isModerator = currentUserRole == UserRole.Moderator.ToString();

            if (!isAuthor && !isAdmin && !isModerator)
            {
                return StatusCode(403, new { error = "Nie masz uprawnień do usunięcia tego wątku." }); // Forbidden
            }

            // Business logic: Prevent deletion if thread has messages (comments)
            if (thread.Messages != null && thread.Messages.Any())
            {
                return StatusCode(400, new { error = "Nie można usunąć wątku posiadającego wiadomości." }); // Bad Request
            }
            
            _context.Threads.Remove(thread);
            await _context.SaveChangesAsync();

            return NoContent(); // 204 No Content for successful deletion
        }

        private bool ThreadExists(int id)
        {
            return _context.Threads.Any(e => e.Id == id);
        }
    }
}
