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
    public class ForumPermissionsController : Controller
    {
        private readonly ForumDbContext _context;

        public ForumPermissionsController(ForumDbContext context)
        {
            _context = context;
        }

        // GET: ForumPermissions
        public async Task<IActionResult> Index()
        {
            var forumDbContext = _context.ForumPermissions.Include(f => f.Forum);
            return View(await forumDbContext.ToListAsync());
        }

        // GET: ForumPermissions/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var forumPermission = await _context.ForumPermissions
                .Include(f => f.Forum)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (forumPermission == null)
            {
                return NotFound();
            }

            return View(forumPermission);
        }

        // GET: ForumPermissions/Create
        public IActionResult Create()
        {
            ViewData["ForumId"] = new SelectList(_context.Forums, "Id", "Name");
            return View();
        }

        // POST: ForumPermissions/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,ForumId,CanRead,CanWrite,AllowAnonymousView,AllowAnonymousPost")] ForumPermission forumPermission)
        {
            if (ModelState.IsValid)
            {
                _context.Add(forumPermission);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["ForumId"] = new SelectList(_context.Forums, "Id", "Name", forumPermission.ForumId);
            return View(forumPermission);
        }

        // GET: ForumPermissions/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var forumPermission = await _context.ForumPermissions.FindAsync(id);
            if (forumPermission == null)
            {
                return NotFound();
            }
            ViewData["ForumId"] = new SelectList(_context.Forums, "Id", "Name", forumPermission.ForumId);
            return View(forumPermission);
        }

        // POST: ForumPermissions/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,ForumId,CanRead,CanWrite,AllowAnonymousView,AllowAnonymousPost")] ForumPermission forumPermission)
        {
            if (id != forumPermission.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(forumPermission);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ForumPermissionExists(forumPermission.Id))
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
            ViewData["ForumId"] = new SelectList(_context.Forums, "Id", "Name", forumPermission.ForumId);
            return View(forumPermission);
        }

        // GET: ForumPermissions/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var forumPermission = await _context.ForumPermissions
                .Include(f => f.Forum)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (forumPermission == null)
            {
                return NotFound();
            }

            return View(forumPermission);
        }

        // POST: ForumPermissions/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var forumPermission = await _context.ForumPermissions.FindAsync(id);
            if (forumPermission != null)
            {
                _context.ForumPermissions.Remove(forumPermission);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ForumPermissionExists(int id)
        {
            return _context.ForumPermissions.Any(e => e.Id == id);
        }
    }
}
