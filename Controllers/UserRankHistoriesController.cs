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
    public class UserRankHistoriesController : Controller
    {
        private readonly ForumDbContext _context;

        public UserRankHistoriesController(ForumDbContext context)
        {
            _context = context;
        }

        // GET: UserRankHistories
        public async Task<IActionResult> Index()
        {
            var forumDbContext = _context.UserRankHistories.Include(u => u.ChangedByUser).Include(u => u.NewRank).Include(u => u.OldRank).Include(u => u.User);
            return View(await forumDbContext.ToListAsync());
        }

        // GET: UserRankHistories/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var userRankHistory = await _context.UserRankHistories
                .Include(u => u.ChangedByUser)
                .Include(u => u.NewRank)
                .Include(u => u.OldRank)
                .Include(u => u.User)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (userRankHistory == null)
            {
                return NotFound();
            }

            return View(userRankHistory);
        }

        // GET: UserRankHistories/Create
        public IActionResult Create()
        {
            ViewData["ChangedBy"] = new SelectList(_context.Users, "Id", "Email");
            ViewData["NewRankId"] = new SelectList(_context.UserRanks, "Id", "Name");
            ViewData["OldRankId"] = new SelectList(_context.UserRanks, "Id", "Name");
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Email");
            return View();
        }

        // POST: UserRankHistories/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,UserId,OldRankId,NewRankId,PostCountAtChange,ChangeReason,ChangedBy,CreatedAt")] UserRankHistory userRankHistory)
        {
            if (ModelState.IsValid)
            {
                _context.Add(userRankHistory);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["ChangedBy"] = new SelectList(_context.Users, "Id", "Email", userRankHistory.ChangedBy);
            ViewData["NewRankId"] = new SelectList(_context.UserRanks, "Id", "Name", userRankHistory.NewRankId);
            ViewData["OldRankId"] = new SelectList(_context.UserRanks, "Id", "Name", userRankHistory.OldRankId);
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Email", userRankHistory.UserId);
            return View(userRankHistory);
        }

        // GET: UserRankHistories/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var userRankHistory = await _context.UserRankHistories.FindAsync(id);
            if (userRankHistory == null)
            {
                return NotFound();
            }
            ViewData["ChangedBy"] = new SelectList(_context.Users, "Id", "Email", userRankHistory.ChangedBy);
            ViewData["NewRankId"] = new SelectList(_context.UserRanks, "Id", "Name", userRankHistory.NewRankId);
            ViewData["OldRankId"] = new SelectList(_context.UserRanks, "Id", "Name", userRankHistory.OldRankId);
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Email", userRankHistory.UserId);
            return View(userRankHistory);
        }

        // POST: UserRankHistories/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,UserId,OldRankId,NewRankId,PostCountAtChange,ChangeReason,ChangedBy,CreatedAt")] UserRankHistory userRankHistory)
        {
            if (id != userRankHistory.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(userRankHistory);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!UserRankHistoryExists(userRankHistory.Id))
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
            ViewData["ChangedBy"] = new SelectList(_context.Users, "Id", "Email", userRankHistory.ChangedBy);
            ViewData["NewRankId"] = new SelectList(_context.UserRanks, "Id", "Name", userRankHistory.NewRankId);
            ViewData["OldRankId"] = new SelectList(_context.UserRanks, "Id", "Name", userRankHistory.OldRankId);
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Email", userRankHistory.UserId);
            return View(userRankHistory);
        }

        // GET: UserRankHistories/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var userRankHistory = await _context.UserRankHistories
                .Include(u => u.ChangedByUser)
                .Include(u => u.NewRank)
                .Include(u => u.OldRank)
                .Include(u => u.User)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (userRankHistory == null)
            {
                return NotFound();
            }

            return View(userRankHistory);
        }

        // POST: UserRankHistories/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var userRankHistory = await _context.UserRankHistories.FindAsync(id);
            if (userRankHistory != null)
            {
                _context.UserRankHistories.Remove(userRankHistory);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool UserRankHistoryExists(int id)
        {
            return _context.UserRankHistories.Any(e => e.Id == id);
        }
    }
}
