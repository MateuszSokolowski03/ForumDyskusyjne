using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ForumDyskusyjne.Data;
using ForumDyskusyjne.Models;

namespace ForumDyskusyjne
{
    public class BannedWordsController : Controller
    {
        private readonly ForumDbContext _context;

        public BannedWordsController(ForumDbContext context)
        {
            _context = context;
        }

        // GET: BannedWords
        public async Task<IActionResult> Index()
        {
            var forumDbContext = _context.BannedWords.Include(b => b.CreatedByUser);
            return View(await forumDbContext.ToListAsync());
        }

        // GET: BannedWords/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var bannedWord = await _context.BannedWords
                .Include(b => b.CreatedByUser)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (bannedWord == null)
            {
                return NotFound();
            }

            return View(bannedWord);
        }

        // GET: BannedWords/Create
        public IActionResult Create()
        {
            ViewData["CreatedBy"] = new SelectList(_context.Users, "Id", "Email");
            return View();
        }

        // POST: BannedWords/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Word,CreatedAt,SeverityLevel,MatchType,IsActive,CreatedBy,UpdatedAt,UsageCount")] BannedWord bannedWord)
        {
            if (ModelState.IsValid)
            {
                _context.Add(bannedWord);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["CreatedBy"] = new SelectList(_context.Users, "Id", "Email", bannedWord.CreatedBy);
            return View(bannedWord);
        }

        // GET: BannedWords/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var bannedWord = await _context.BannedWords.FindAsync(id);
            if (bannedWord == null)
            {
                return NotFound();
            }
            ViewData["CreatedBy"] = new SelectList(_context.Users, "Id", "Email", bannedWord.CreatedBy);
            return View(bannedWord);
        }

        // POST: BannedWords/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Word,CreatedAt,SeverityLevel,MatchType,IsActive,CreatedBy,UpdatedAt,UsageCount")] BannedWord bannedWord)
        {
            if (id != bannedWord.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(bannedWord);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!BannedWordExists(bannedWord.Id))
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
            ViewData["CreatedBy"] = new SelectList(_context.Users, "Id", "Email", bannedWord.CreatedBy);
            return View(bannedWord);
        }

        // GET: BannedWords/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var bannedWord = await _context.BannedWords
                .Include(b => b.CreatedByUser)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (bannedWord == null)
            {
                return NotFound();
            }

            return View(bannedWord);
        }

        // POST: BannedWords/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var bannedWord = await _context.BannedWords.FindAsync(id);
            if (bannedWord != null)
            {
                _context.BannedWords.Remove(bannedWord);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool BannedWordExists(int id)
        {
            return _context.BannedWords.Any(e => e.Id == id);
        }
    }
}
