using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ForumDyskusyjne.Data;
using ForumDyskusyjne.Models;
using System.Security.Claims;

namespace ForumDyskusyjne.Controllers.Api
{
    [Route("api/users")]
    [ApiController]
    [Authorize] // Wymaga logowania dla większości akcji
    public class UsersApiController : ControllerBase
    {
        private readonly ForumDbContext _context;
        private readonly IWebHostEnvironment _env; // Potrzebne do zapisu plików

        // Wstrzykujemy IWebHostEnvironment
        public UsersApiController(ForumDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // --- 1. ENDPOINT DO UPLOADU AVATARA (NAPRAWA BŁĘDU 403) ---
        [HttpPut("avatar")]
        public async Task<IActionResult> UploadAvatar(IFormFile file)
        {
            try 
            {
                var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(userIdStr, out int userId)) return Unauthorized();

                if (file == null || file.Length == 0)
                    return BadRequest(new { error = "Nie przesłano pliku" });

                // Walidacja rozszerzenia
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                var extension = Path.GetExtension(file.FileName).ToLower();
                if (!allowedExtensions.Contains(extension))
                    return BadRequest(new { error = "Niedozwolony format pliku" });

                // Przygotowanie ścieżki: wwwroot/uploads/avatars
                var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "avatars");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                // Unikalna nazwa pliku
                var uniqueFileName = $"{userId}_{Guid.NewGuid()}{extension}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                // Zapis na dysk
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Aktualizacja w bazie
                var user = await _context.Users.FindAsync(userId);
                if (user == null) return NotFound(new { error = "Użytkownik nie istnieje" });

                // Opcjonalnie: Usuń stary avatar z dysku, jeśli istnieje
                // ...

                user.AvatarUrl = $"/uploads/avatars/{uniqueFileName}";
                await _context.SaveChangesAsync();

                return Ok(new { message = "Awatar zaktualizowany", avatarUrl = user.AvatarUrl });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Błąd serwera: " + ex.Message });
            }
        }

        // --- 2. ENDPOINT DO NAGŁÓWKA (NAPRAWA WYŚWIETLANIA AVATARA) ---
        // GET: api/users/{id}
        [HttpGet("{id}")]
        [AllowAnonymous] // Pozwalamy pobrać avatar nawet niezalogowanym (np. przy postach)
        public async Task<IActionResult> GetUserDetails(int id)
        {
            var user = await _context.Users
                .Include(u => u.CurrentRank)
                .Where(u => u.Id == id)
                .Select(u => new 
                {
                    u.Id,
                    u.Username,
                    u.AvatarUrl,
                    u.CreatedAt,
                    Role = u.Role.ToString(),
                    CurrentRank = u.CurrentRank != null ? u.CurrentRank.Name : "Brak rangi"
                })
                .FirstOrDefaultAsync();

            if (user == null) return NotFound(new { error = "Użytkownik nie istnieje" });

            return Ok(user);
        }

        // --- 3. ENDPOINT DLA PROFILU (PEŁNE DANE) ---
        // GET: api/users/profile/{id}
        [HttpGet("profile/{id}")]
        public async Task<IActionResult> GetUserProfile(int id)
        {
            var user = await _context.Users
                .Include(u => u.CurrentRank)
                .Where(u => u.Id == id)
                .Select(u => new 
                {
                    u.Id,
                    u.Username,
                    u.Email,
                    u.AvatarUrl,
                    u.CreatedAt,
                    u.LastActivityAt,
                    CurrentRank = u.CurrentRank != null ? u.CurrentRank.Name : "Brak rangi"
                })
                .FirstOrDefaultAsync();

            if (user == null) return NotFound(new { error = "Użytkownik nie istnieje" });

            return Ok(user);
        }

        // --- METODY ADMINISTRACYJNE (BEZ ZMIAN) ---

        // GET: api/users
        [HttpGet]
        [Authorize(Roles = "Admin")] 
        public async Task<IActionResult> GetUsers([FromQuery] string? search, [FromQuery] string? role, [FromQuery] string? status, [FromQuery] int? rankId)
        {
            var query = _context.Users
                .Include(u => u.CurrentRank)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                search = search.ToLower();
                query = query.Where(u => u.Username.ToLower().Contains(search) || u.Email.ToLower().Contains(search));
            }

            if (!string.IsNullOrEmpty(role))
            {
                if (Enum.TryParse<UserRole>(role, true, out var roleEnum))
                    query = query.Where(u => u.Role == roleEnum);
            }

            if (!string.IsNullOrEmpty(status))
            {
                if (status == "blocked") query = query.Where(u => u.IsBanned);
                if (status == "active") query = query.Where(u => !u.IsBanned);
            }

            // NOWE: Filtrowanie po randze
            if (rankId.HasValue)
            {
                query = query.Where(u => u.CurrentRankId == rankId.Value);
            }

            var users = await query
                .OrderByDescending(u => u.CreatedAt)
                .Select(u => new {
                    u.Id,
                    u.Username,
                    u.Email,
                    u.AvatarUrl,
                    Role = u.Role.ToString(),
                    u.IsBanned,
                    u.CreatedAt,
                    u.LastActivityAt,
                    CurrentRank = u.CurrentRank != null ? u.CurrentRank.Name : "Brak",
                    CurrentRankId = u.CurrentRankId
                })
                .ToListAsync();

            return Ok(users);
        }

        // GET: api/users/ranks
        [HttpGet("ranks")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetRanks()
        {
            var ranks = await _context.UserRanks
                .OrderBy(r => r.MinMessages)
                .Select(r => new { r.Id, r.Name, r.MinMessages })
                .ToListAsync();
            return Ok(ranks);
        }

        // PUT: api/users/{id}
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] UserUpdateDto dto)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound(new { error = "Użytkownik nie istnieje" });

            if (dto.IsBanned != user.IsBanned) user.IsBanned = dto.IsBanned;

            if (!string.IsNullOrEmpty(dto.Role) && Enum.TryParse<UserRole>(dto.Role, true, out var newRole))
            {
                if (user.Role != newRole) user.Role = newRole;
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Użytkownik zaktualizowany" });
        }

        // PUT: api/users/{id}/rank
        [HttpPut("{id}/rank")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ChangeRank(int id, [FromBody] int rankId)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound(new { error = "Użytkownik nie istnieje" });

            var rank = await _context.UserRanks.FindAsync(rankId);
            if (rank == null) return NotFound(new { error = "Ranga nie istnieje" });

            user.CurrentRankId = rankId;
            await _context.SaveChangesAsync();
            return Ok(new { message = "Ranga zmieniona" });
        }

        // DELETE: api/users/{id}
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (currentUserId == id.ToString())
                return BadRequest(new { error = "Nie możesz usunąć własnego konta." });

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Użytkownik usunięty" });
        }

        public class UserUpdateDto
        {
            public int Id { get; set; }
            public string Username { get; set; }
            public string Email { get; set; }
            public bool IsBanned { get; set; }
            public string Role { get; set; }
        }
    }
}