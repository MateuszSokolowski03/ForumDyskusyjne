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
    public class ForumModeratorsController : Controller
    {
        private readonly ForumDbContext _context;

        public ForumModeratorsController(ForumDbContext context)
        {
            _context = context;
        }

        // GET: ForumModerators
        public async Task<IActionResult> Index()
        {
            var forumDbContext = _context.ForumModerators.Include(f => f.AssignedByUser).Include(f => f.Forum).Include(f => f.User);
            return View(await forumDbContext.ToListAsync());
        }

        // GET: ForumModerators/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var forumModerator = await _context.ForumModerators
                .Include(f => f.AssignedByUser)
                .Include(f => f.Forum)
                .Include(f => f.User)
                .FirstOrDefaultAsync(m => m.ForumId == id);
            if (forumModerator == null)
            {
                return NotFound();
            }

            return View(forumModerator);
        }

        // GET: ForumModerators/Create
        public IActionResult Create()
        {
            ViewData["AssignedBy"] = new SelectList(_context.Users, "Id", "Email");
            ViewData["ForumId"] = new SelectList(_context.Forums, "Id", "Name");
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Email");
            return View();
        }

        // POST: ForumModerators/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ForumId,UserId,AssignedBy,AssignedAt")] ForumModerator forumModerator)
        {
            if (ModelState.IsValid)
            {
                _context.Add(forumModerator);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["AssignedBy"] = new SelectList(_context.Users, "Id", "Email", forumModerator.AssignedBy);
            ViewData["ForumId"] = new SelectList(_context.Forums, "Id", "Name", forumModerator.ForumId);
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Email", forumModerator.UserId);
            return View(forumModerator);
        }

        // GET: ForumModerators/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var forumModerator = await _context.ForumModerators.FindAsync(id);
            if (forumModerator == null)
            {
                return NotFound();
            }
            ViewData["AssignedBy"] = new SelectList(_context.Users, "Id", "Email", forumModerator.AssignedBy);
            ViewData["ForumId"] = new SelectList(_context.Forums, "Id", "Name", forumModerator.ForumId);
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Email", forumModerator.UserId);
            return View(forumModerator);
        }

        // POST: ForumModerators/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ForumId,UserId,AssignedBy,AssignedAt")] ForumModerator forumModerator)
        {
            if (id != forumModerator.ForumId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(forumModerator);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ForumModeratorExists(forumModerator.ForumId))
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
            ViewData["AssignedBy"] = new SelectList(_context.Users, "Id", "Email", forumModerator.AssignedBy);
            ViewData["ForumId"] = new SelectList(_context.Forums, "Id", "Name", forumModerator.ForumId);
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Email", forumModerator.UserId);
            return View(forumModerator);
        }

        // GET: ForumModerators/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var forumModerator = await _context.ForumModerators
                .Include(f => f.AssignedByUser)
                .Include(f => f.Forum)
                .Include(f => f.User)
                .FirstOrDefaultAsync(m => m.ForumId == id);
            if (forumModerator == null)
            {
                return NotFound();
            }

            return View(forumModerator);
        }

        // POST: ForumModerators/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var forumModerator = await _context.ForumModerators.FindAsync(id);
            if (forumModerator != null)
            {
                _context.ForumModerators.Remove(forumModerator);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ForumModeratorExists(int id)
        {
            return _context.ForumModerators.Any(e => e.ForumId == id);
        }
    }
}
