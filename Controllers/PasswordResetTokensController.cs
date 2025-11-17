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
    public class PasswordResetTokensController : Controller
    {
        private readonly ForumDbContext _context;

        public PasswordResetTokensController(ForumDbContext context)
        {
            _context = context;
        }

        // GET: PasswordResetTokens
        public async Task<IActionResult> Index()
        {
            var forumDbContext = _context.PasswordResetTokens.Include(p => p.User);
            return View(await forumDbContext.ToListAsync());
        }

        // GET: PasswordResetTokens/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var passwordResetToken = await _context.PasswordResetTokens
                .Include(p => p.User)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (passwordResetToken == null)
            {
                return NotFound();
            }

            return View(passwordResetToken);
        }

        // GET: PasswordResetTokens/Create
        public IActionResult Create()
        {
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Email");
            return View();
        }

        // POST: PasswordResetTokens/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,UserId,Token,ExpiresAt,UsedAt,IpAddress,UserAgent,CreatedAt,IsUsed")] PasswordResetToken passwordResetToken)
        {
            if (ModelState.IsValid)
            {
                _context.Add(passwordResetToken);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Email", passwordResetToken.UserId);
            return View(passwordResetToken);
        }

        // GET: PasswordResetTokens/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var passwordResetToken = await _context.PasswordResetTokens.FindAsync(id);
            if (passwordResetToken == null)
            {
                return NotFound();
            }
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Email", passwordResetToken.UserId);
            return View(passwordResetToken);
        }

        // POST: PasswordResetTokens/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,UserId,Token,ExpiresAt,UsedAt,IpAddress,UserAgent,CreatedAt,IsUsed")] PasswordResetToken passwordResetToken)
        {
            if (id != passwordResetToken.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(passwordResetToken);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PasswordResetTokenExists(passwordResetToken.Id))
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
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Email", passwordResetToken.UserId);
            return View(passwordResetToken);
        }

        // GET: PasswordResetTokens/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var passwordResetToken = await _context.PasswordResetTokens
                .Include(p => p.User)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (passwordResetToken == null)
            {
                return NotFound();
            }

            return View(passwordResetToken);
        }

        // POST: PasswordResetTokens/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var passwordResetToken = await _context.PasswordResetTokens.FindAsync(id);
            if (passwordResetToken != null)
            {
                _context.PasswordResetTokens.Remove(passwordResetToken);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool PasswordResetTokenExists(int id)
        {
            return _context.PasswordResetTokens.Any(e => e.Id == id);
        }
    }
}
