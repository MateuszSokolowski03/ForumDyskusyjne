using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ForumDyskusyjne.Data;
using ForumDyskusyjne.Models;

namespace ForumDyskusyjne.Controllers
{
    [Route("api/admin/[controller]")]
    [ApiController]
    public class BannedWordsApiController : ControllerBase
    {
        private readonly ForumDbContext _context;

        public BannedWordsApiController(ForumDbContext context)
        {
            _context = context;
        }

        // GET: api/admin/banned-words
        [HttpGet]
        public async Task<ActionResult<IEnumerable<BannedWord>>> GetBannedWords()
        {
            try
            {
                var words = await _context.BannedWords
                    .OrderByDescending(b => b.CreatedAt)
                    .ToListAsync();
                return Ok(words);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // GET: api/admin/banned-words/5
        [HttpGet("{id}")]
        public async Task<ActionResult<BannedWord>> GetBannedWord(int id)
        {
            try
            {
                var bannedWord = await _context.BannedWords.FindAsync(id);

                if (bannedWord == null)
                {
                    return NotFound(new { success = false, message = "Słowo nie znalezione" });
                }

                return Ok(bannedWord);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // POST: api/admin/banned-words
        [HttpPost]
        public async Task<ActionResult<BannedWord>> PostBannedWord([FromBody] BannedWord bannedWord)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(bannedWord.Word))
                {
                    return BadRequest(new { success = false, message = "Słowo nie może być puste" });
                }

                // Check if word already exists
                var existingWord = await _context.BannedWords
                    .FirstOrDefaultAsync(b => b.Word.ToLower() == bannedWord.Word.ToLower());

                if (existingWord != null)
                {
                    return BadRequest(new { success = false, message = "To słowo już istnieje w bazie" });
                }

                bannedWord.CreatedAt = DateTime.UtcNow;
                bannedWord.UpdatedAt = DateTime.UtcNow;
                bannedWord.IsActive = true;

                _context.BannedWords.Add(bannedWord);
                await _context.SaveChangesAsync();

                return CreatedAtAction("GetBannedWord", new { id = bannedWord.Id }, new { success = true, data = bannedWord });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // PUT: api/admin/banned-words/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutBannedWord(int id, [FromBody] BannedWord bannedWord)
        {
            try
            {
                if (id != bannedWord.Id)
                {
                    return BadRequest(new { success = false, message = "ID nie pasuje" });
                }

                var existingWord = await _context.BannedWords.FindAsync(id);
                if (existingWord == null)
                {
                    return NotFound(new { success = false, message = "Słowo nie znalezione" });
                }

                existingWord.Word = bannedWord.Word;
                existingWord.SeverityLevel = bannedWord.SeverityLevel;
                existingWord.MatchType = bannedWord.MatchType;
                existingWord.IsActive = bannedWord.IsActive;
                existingWord.UpdatedAt = DateTime.UtcNow;

                _context.BannedWords.Update(existingWord);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, data = existingWord });
            }
            catch (DbUpdateConcurrencyException)
            {
                return StatusCode(500, new { success = false, message = "Błąd konkurencji bazy danych" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // DELETE: api/admin/banned-words/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteBannedWord(int id)
        {
            try
            {
                var bannedWord = await _context.BannedWords.FindAsync(id);
                if (bannedWord == null)
                {
                    return NotFound(new { success = false, message = "Słowo nie znalezione" });
                }

                _context.BannedWords.Remove(bannedWord);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Słowo zostało usunięte" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        private bool BannedWordExists(int id)
        {
            return _context.BannedWords.Any(e => e.Id == id);
        }
    }
}
