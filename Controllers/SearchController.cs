using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using ForumDyskusyjne.Data;  
using ForumDyskusyjne.Models; 

namespace ForumDyskusyjne.Controllers
{
    [Route("api/[controller]")] // To automatycznie stworzy ścieżkę: /api/search
    [ApiController]
    public class SearchController : ControllerBase
    {
        private readonly ForumDbContext _context;

        public SearchController(ForumDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Search([FromQuery] string q)
        {
            // 1. Walidacja: jeśli zapytanie jest puste lub za krótkie, zwróć puste tablice
            if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
            {
                return Ok(new { forums = new List<object>(), threads = new List<object>() });
            }

            // Normalizacja do małych liter dla lepszego wyszukiwania (chyba że masz Collation Case-Insensitive w bazie)
            var query = q.ToLower().Trim();

            // ==========================================
            // 2. WYSZUKIWANIE W FORACH (KATEGORIACH)
            // ==========================================
            var forums = await _context.Forums
                .AsNoTracking()
                .Where(f => f.Name.ToLower().Contains(query) ||
                           (f.Description != null && f.Description.ToLower().Contains(query)))
                .Select(f => new
                {
                    id = f.Id,
                    name = f.Name,
                    description = f.Description
                })
                .Take(5)
                .ToListAsync();

// ==========================================
            // 3. WYSZUKIWANIE W WĄTKACH
            // ==========================================
            var threads = await _context.Threads
                .AsNoTracking()
                .Include(t => t.Author)
                .Where(t => t.Title.ToLower().Contains(query))
                .OrderByDescending(t => t.CreatedAt)
                .Select(t => new
                {
                    id = t.Id,
                    title = t.Title,
                    // ZMIANA TUTAJ: author -> authorName (żeby pasowało do JS)
                    authorName = t.Author != null ? t.Author.Username : "Nieznany",
                    repliesCount = t.RepliesCount,
                    views = t.Views,
                    createdAt = t.CreatedAt
                })
                .Take(10)
                .ToListAsync();

            // 4. Zwracamy JSON pasujący do Twojego main.js
            return Ok(new
            {
                forums = forums,
                threads = threads
            });
        }
    }
}