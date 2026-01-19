using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ForumDyskusyjne.Data;
using ForumDyskusyjne.Models;

namespace ForumDyskusyjne.Controllers
{
    public class MessagesController : Controller
    {
        private readonly ForumDbContext _context;

        public MessagesController(ForumDbContext context)
        {
            _context = context;
        }

        // GET: Messages
        public async Task<IActionResult> Index()
        {
            var forumDbContext = _context.Messages.Include(m => m.Author).Include(m => m.Thread);
            return View(await forumDbContext.ToListAsync());
        }

        // GET: Messages/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var message = await _context.Messages
                .Include(m => m.Author)
                .Include(m => m.Thread)
                .FirstOrDefaultAsync(m => m.Id == id);
            
            if (message == null) return NotFound();

            return View(message);
        }

        // GET: Messages/Create
        public IActionResult Create()
        {
            ViewData["AuthorId"] = new SelectList(_context.Users, "Id", "Email");
            ViewData["ThreadId"] = new SelectList(_context.Threads, "Id", "Title");
            return View();
        }

        // POST: Messages/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,ThreadId,AuthorId,Content,CreatedAt,IsEdited,EditedAt")] Message message)
        {
            if (ModelState.IsValid)
            {
                // 1. Zapisz wiadomość
                _context.Add(message);
                
                // 2. LOGIKA RANG: Zwiększ licznik i sprawdź rangę
                var user = await _context.Users.FindAsync(message.AuthorId);
                if (user != null)
                {
                    user.PostCount++; // Zwiększamy licznik postów
                    
                    // Pobierz obecną rangę, żeby sprawdzić, czy jest "ręczna" (np. Admin)
                    var currentRank = await _context.UserRanks.FindAsync(user.CurrentRankId);
                    
                    // Zmieniamy rangę TYLKO wtedy, gdy obecna ranga NIE jest ustawiana ręcznie.
                    // Dzięki temu Admin/Moderator nie straci rangi po napisaniu posta.
                    if (currentRank != null && !currentRank.CanBeSetManually)
                    {
                        // Szukamy najwyższej możliwej rangi automatycznej dla tej liczby postów
                        var newRank = await _context.UserRanks
                            .Where(r => r.MinMessages <= user.PostCount && !r.CanBeSetManually)
                            .OrderByDescending(r => r.MinMessages)
                            .FirstOrDefaultAsync();

                        // Jeśli znaleziono nową rangę i jest inna niż obecna -> Aktualizuj
                        if (newRank != null && newRank.Id != user.CurrentRankId)
                        {
                            user.CurrentRankId = newRank.Id;
                        }
                    }
                }

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["AuthorId"] = new SelectList(_context.Users, "Id", "Email", message.AuthorId);
            ViewData["ThreadId"] = new SelectList(_context.Threads, "Id", "Title", message.ThreadId);
            return View(message);
        }

        // GET: Messages/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var message = await _context.Messages.FindAsync(id);
            if (message == null) return NotFound();
            
            ViewData["AuthorId"] = new SelectList(_context.Users, "Id", "Email", message.AuthorId);
            ViewData["ThreadId"] = new SelectList(_context.Threads, "Id", "Title", message.ThreadId);
            return View(message);
        }

        // POST: Messages/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,ThreadId,AuthorId,Content,CreatedAt,IsEdited,EditedAt")] Message message)
        {
            if (id != message.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(message);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MessageExists(message.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["AuthorId"] = new SelectList(_context.Users, "Id", "Email", message.AuthorId);
            ViewData["ThreadId"] = new SelectList(_context.Threads, "Id", "Title", message.ThreadId);
            return View(message);
        }

        // GET: Messages/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var message = await _context.Messages
                .Include(m => m.Author)
                .Include(m => m.Thread)
                .FirstOrDefaultAsync(m => m.Id == id);
            
            if (message == null) return NotFound();

            return View(message);
        }

        // POST: Messages/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var message = await _context.Messages.FindAsync(id);
            if (message != null)
            {
                // 1. Zmniejsz licznik postów autora (opcjonalne, ale uczciwe)
                var user = await _context.Users.FindAsync(message.AuthorId);
                if (user != null && user.PostCount > 0)
                {
                    user.PostCount--;
                    // Tutaj zazwyczaj NIE degradujemy rangi automatycznie, 
                    // żeby uniknąć efektu "skakania" rangi przy usuwaniu jednego posta.
                }

                _context.Messages.Remove(message);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool MessageExists(int id)
        {
            return _context.Messages.Any(e => e.Id == id);
        }
    }
}