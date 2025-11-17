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
    public class UserRanksController : Controller
    {
        private readonly ForumDbContext _context;

        public UserRanksController(ForumDbContext context)
        {
            _context = context;
        }

        // GET: UserRanks
        public async Task<IActionResult> Index()
        {
            return View(await _context.UserRanks.ToListAsync());
        }

        // GET: UserRanks/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var userRank = await _context.UserRanks
                .FirstOrDefaultAsync(m => m.Id == id);
            if (userRank == null)
            {
                return NotFound();
            }

            return View(userRank);
        }

        // GET: UserRanks/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: UserRanks/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,MinMessages,CanBeSetManually,MaxMessages,Color,Icon,Description,IsActive,SortOrder,CreatedAt,UpdatedAt")] UserRank userRank)
        {
            if (ModelState.IsValid)
            {
                _context.Add(userRank);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(userRank);
        }

        // GET: UserRanks/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var userRank = await _context.UserRanks.FindAsync(id);
            if (userRank == null)
            {
                return NotFound();
            }
            return View(userRank);
        }

        // POST: UserRanks/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,MinMessages,CanBeSetManually,MaxMessages,Color,Icon,Description,IsActive,SortOrder,CreatedAt,UpdatedAt")] UserRank userRank)
        {
            if (id != userRank.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(userRank);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!UserRankExists(userRank.Id))
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
            return View(userRank);
        }

        // GET: UserRanks/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var userRank = await _context.UserRanks
                .FirstOrDefaultAsync(m => m.Id == id);
            if (userRank == null)
            {
                return NotFound();
            }

            return View(userRank);
        }

        // POST: UserRanks/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var userRank = await _context.UserRanks.FindAsync(id);
            if (userRank != null)
            {
                _context.UserRanks.Remove(userRank);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool UserRankExists(int id)
        {
            return _context.UserRanks.Any(e => e.Id == id);
        }
    }
}
