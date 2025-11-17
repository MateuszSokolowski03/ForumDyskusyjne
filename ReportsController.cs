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
    public class ReportsController : Controller
    {
        private readonly ForumDbContext _context;

        public ReportsController(ForumDbContext context)
        {
            _context = context;
        }

        // GET: Reports
        public async Task<IActionResult> Index()
        {
            var forumDbContext = _context.Reports.Include(r => r.HandledByUser).Include(r => r.ReportedMessage).Include(r => r.Reporter);
            return View(await forumDbContext.ToListAsync());
        }

        // GET: Reports/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var report = await _context.Reports
                .Include(r => r.HandledByUser)
                .Include(r => r.ReportedMessage)
                .Include(r => r.Reporter)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (report == null)
            {
                return NotFound();
            }

            return View(report);
        }

        // GET: Reports/Create
        public IActionResult Create()
        {
            ViewData["HandledBy"] = new SelectList(_context.Users, "Id", "Email");
            ViewData["ReportedMessageId"] = new SelectList(_context.Messages, "Id", "Content");
            ViewData["ReporterId"] = new SelectList(_context.Users, "Id", "Email");
            return View();
        }

        // POST: Reports/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,ReporterId,ReportedMessageId,Reason,Status,CreatedAt,HandledBy,HandledAt,AdminNotes")] Report report)
        {
            if (ModelState.IsValid)
            {
                _context.Add(report);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["HandledBy"] = new SelectList(_context.Users, "Id", "Email", report.HandledBy);
            ViewData["ReportedMessageId"] = new SelectList(_context.Messages, "Id", "Content", report.ReportedMessageId);
            ViewData["ReporterId"] = new SelectList(_context.Users, "Id", "Email", report.ReporterId);
            return View(report);
        }

        // GET: Reports/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var report = await _context.Reports.FindAsync(id);
            if (report == null)
            {
                return NotFound();
            }
            ViewData["HandledBy"] = new SelectList(_context.Users, "Id", "Email", report.HandledBy);
            ViewData["ReportedMessageId"] = new SelectList(_context.Messages, "Id", "Content", report.ReportedMessageId);
            ViewData["ReporterId"] = new SelectList(_context.Users, "Id", "Email", report.ReporterId);
            return View(report);
        }

        // POST: Reports/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,ReporterId,ReportedMessageId,Reason,Status,CreatedAt,HandledBy,HandledAt,AdminNotes")] Report report)
        {
            if (id != report.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(report);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ReportExists(report.Id))
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
            ViewData["HandledBy"] = new SelectList(_context.Users, "Id", "Email", report.HandledBy);
            ViewData["ReportedMessageId"] = new SelectList(_context.Messages, "Id", "Content", report.ReportedMessageId);
            ViewData["ReporterId"] = new SelectList(_context.Users, "Id", "Email", report.ReporterId);
            return View(report);
        }

        // GET: Reports/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var report = await _context.Reports
                .Include(r => r.HandledByUser)
                .Include(r => r.ReportedMessage)
                .Include(r => r.Reporter)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (report == null)
            {
                return NotFound();
            }

            return View(report);
        }

        // POST: Reports/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var report = await _context.Reports.FindAsync(id);
            if (report != null)
            {
                _context.Reports.Remove(report);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ReportExists(int id)
        {
            return _context.Reports.Any(e => e.Id == id);
        }
    }
}
