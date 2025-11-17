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
    public class AdminActionsController : Controller
    {
        private readonly ForumDbContext _context;

        public AdminActionsController(ForumDbContext context)
        {
            _context = context;
        }

        // GET: AdminActions
        public async Task<IActionResult> Index()
        {
            var forumDbContext = _context.AdminActions.Include(a => a.Admin);
            return View(await forumDbContext.ToListAsync());
        }

        // GET: AdminActions/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var adminAction = await _context.AdminActions
                .Include(a => a.Admin)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (adminAction == null)
            {
                return NotFound();
            }

            return View(adminAction);
        }

        // GET: AdminActions/Create
        public IActionResult Create()
        {
            ViewData["AdminId"] = new SelectList(_context.Users, "Id", "Email");
            return View();
        }

        // POST: AdminActions/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,AdminId,ActionType,TargetType,TargetId,Description,CreatedAt")] AdminAction adminAction)
        {
            if (ModelState.IsValid)
            {
                _context.Add(adminAction);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["AdminId"] = new SelectList(_context.Users, "Id", "Email", adminAction.AdminId);
            return View(adminAction);
        }

        // GET: AdminActions/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var adminAction = await _context.AdminActions.FindAsync(id);
            if (adminAction == null)
            {
                return NotFound();
            }
            ViewData["AdminId"] = new SelectList(_context.Users, "Id", "Email", adminAction.AdminId);
            return View(adminAction);
        }

        // POST: AdminActions/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,AdminId,ActionType,TargetType,TargetId,Description,CreatedAt")] AdminAction adminAction)
        {
            if (id != adminAction.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(adminAction);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!AdminActionExists(adminAction.Id))
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
            ViewData["AdminId"] = new SelectList(_context.Users, "Id", "Email", adminAction.AdminId);
            return View(adminAction);
        }

        // GET: AdminActions/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var adminAction = await _context.AdminActions
                .Include(a => a.Admin)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (adminAction == null)
            {
                return NotFound();
            }

            return View(adminAction);
        }

        // POST: AdminActions/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var adminAction = await _context.AdminActions.FindAsync(id);
            if (adminAction != null)
            {
                _context.AdminActions.Remove(adminAction);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool AdminActionExists(int id)
        {
            return _context.AdminActions.Any(e => e.Id == id);
        }
    }
}
