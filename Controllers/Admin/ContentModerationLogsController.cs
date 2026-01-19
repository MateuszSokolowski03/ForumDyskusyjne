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
    public class ContentModerationLogsController : Controller
    {
        private readonly ForumDbContext _context;

        public ContentModerationLogsController(ForumDbContext context)
        {
            _context = context;
        }

        // GET: ContentModerationLogs
        public async Task<IActionResult> Index()
        {
            var forumDbContext = _context.ContentModerationLogs.Include(c => c.BannedWord).Include(c => c.Message).Include(c => c.User);
            return View(await forumDbContext.ToListAsync());
        }

        // GET: ContentModerationLogs/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var contentModerationLog = await _context.ContentModerationLogs
                .Include(c => c.BannedWord)
                .Include(c => c.Message)
                .Include(c => c.User)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (contentModerationLog == null)
            {
                return NotFound();
            }

            return View(contentModerationLog);
        }

        // GET: ContentModerationLogs/Create
        public IActionResult Create()
        {
            ViewData["BannedWordId"] = new SelectList(_context.BannedWords, "Id", "Word");
            ViewData["MessageId"] = new SelectList(_context.Messages, "Id", "Content");
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Email");
            return View();
        }

        // POST: ContentModerationLogs/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,UserId,MessageId,BannedWordId,DetectedText,ActionTaken,CreatedAt")] ContentModerationLog contentModerationLog)
        {
            if (ModelState.IsValid)
            {
                _context.Add(contentModerationLog);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["BannedWordId"] = new SelectList(_context.BannedWords, "Id", "Word", contentModerationLog.BannedWordId);
            ViewData["MessageId"] = new SelectList(_context.Messages, "Id", "Content", contentModerationLog.MessageId);
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Email", contentModerationLog.UserId);
            return View(contentModerationLog);
        }

        // GET: ContentModerationLogs/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var contentModerationLog = await _context.ContentModerationLogs.FindAsync(id);
            if (contentModerationLog == null)
            {
                return NotFound();
            }
            ViewData["BannedWordId"] = new SelectList(_context.BannedWords, "Id", "Word", contentModerationLog.BannedWordId);
            ViewData["MessageId"] = new SelectList(_context.Messages, "Id", "Content", contentModerationLog.MessageId);
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Email", contentModerationLog.UserId);
            return View(contentModerationLog);
        }

        // POST: ContentModerationLogs/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,UserId,MessageId,BannedWordId,DetectedText,ActionTaken,CreatedAt")] ContentModerationLog contentModerationLog)
        {
            if (id != contentModerationLog.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(contentModerationLog);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ContentModerationLogExists(contentModerationLog.Id))
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
            ViewData["BannedWordId"] = new SelectList(_context.BannedWords, "Id", "Word", contentModerationLog.BannedWordId);
            ViewData["MessageId"] = new SelectList(_context.Messages, "Id", "Content", contentModerationLog.MessageId);
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Email", contentModerationLog.UserId);
            return View(contentModerationLog);
        }

        // GET: ContentModerationLogs/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var contentModerationLog = await _context.ContentModerationLogs
                .Include(c => c.BannedWord)
                .Include(c => c.Message)
                .Include(c => c.User)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (contentModerationLog == null)
            {
                return NotFound();
            }

            return View(contentModerationLog);
        }

        // POST: ContentModerationLogs/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var contentModerationLog = await _context.ContentModerationLogs.FindAsync(id);
            if (contentModerationLog != null)
            {
                _context.ContentModerationLogs.Remove(contentModerationLog);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ContentModerationLogExists(int id)
        {
            return _context.ContentModerationLogs.Any(e => e.Id == id);
        }
    }
}
