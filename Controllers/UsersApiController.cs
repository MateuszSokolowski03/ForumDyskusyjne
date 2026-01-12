using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;
using ForumDyskusyjne.Data;
using ForumDyskusyjne.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace ForumDyskusyjne.Controllers
{
    [Route("api/users")]
    [ApiController]
    public class UsersApiController : ControllerBase
    {
        private readonly ForumDbContext _context;

        public UsersApiController(ForumDbContext context)
        {
            _context = context;
        }

        // GET: api/users - pobranie listy wszystkich użytkowników
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> GetAllUsers()
        {
            try
            {
                var users = await _context.Users
                    .Select(u => new
                    {
                        u.Id,
                        u.Username,
                        u.Email,
                        u.AvatarUrl,
                        u.CreatedAt
                    })
                    .ToListAsync();

                return Ok(users);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // GET: api/users/profile/{id}
        [AllowAnonymous]
        [HttpGet("profile/{id}")]
        public async Task<IActionResult> GetProfile(int id)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.CurrentRank)
                    .FirstOrDefaultAsync(u => u.Id == id);

                if (user == null)
                    return NotFound(new { error = "Użytkownik nie znaleziony" });

                return Ok(new
                {
                    user.Id,
                    user.Username,
                    user.Email,
                    user.Bio,
                    user.AvatarUrl,
                    user.CreatedAt,
                    user.PostCount,
                    currentRank = user.CurrentRank?.Name
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // PUT: api/users/update
        [Authorize]
        [HttpPut("update")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateUserRequest request)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(userIdClaim, out var userId))
                    return Unauthorized(new { error = "Użytkownik nie zalogowany" });

                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                    return NotFound(new { error = "Użytkownik nie znaleziony" });

                if (!string.IsNullOrWhiteSpace(request.Bio))
                    user.Bio = request.Bio.Trim();

                if (request.MessagesPerPage.HasValue && request.MessagesPerPage > 0)
                    user.MessagesPerPage = request.MessagesPerPage.Value;

                if (request.ThreadsPerPage.HasValue && request.ThreadsPerPage > 0)
                    user.ThreadsPerPage = request.ThreadsPerPage.Value;

                if (request.AutoLogoutMinutes.HasValue && request.AutoLogoutMinutes > 0)
                    user.AutoLogoutMinutes = request.AutoLogoutMinutes.Value;

                _context.Users.Update(user);
                await _context.SaveChangesAsync();
                return Ok(new { message = "Profil zaktualizowany", user = new { user.Id, user.Username, user.Bio } });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // GET: api/users/{id}
        [AllowAnonymous]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetUser(int id)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.CurrentRank)
                    .FirstOrDefaultAsync(u => u.Id == id);

                if (user == null)
                    return NotFound(new { error = "Użytkownik nie znaleziony" });

                return Ok(new
                {
                    user.Id,
                    user.Username,
                    user.Email,
                    user.AvatarUrl,
                    user.Bio,
                    user.CreatedAt,
                    user.PostCount,
                    user.IsBanned,
                    currentRank = user.CurrentRank?.Name,
                    lastActivityAt = user.LastActivityAt
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // PUT: api/users/avatar
        [Authorize]
        [HttpPut("avatar")]
        public async Task<IActionResult> UpdateAvatar(IFormFile? file)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(userIdClaim, out var userId))
                    return Unauthorized(new { error = "Użytkownik nie zalogowany" });

                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                    return NotFound(new { error = "Użytkownik nie znaleziony" });

                // Jeśli przesłano plik
                if (file != null && file.Length > 0)
                {
                    if (file.Length > 5 * 1024 * 1024) // 5MB
                        return BadRequest(new { error = "Plik za duży (maksymalnie 5MB)" });

                    var extension = Path.GetExtension(file.FileName).ToLower();
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                    if (!allowedExtensions.Contains(extension))
                        return BadRequest(new { error = "Nieobsługiwany format - dozwolone: jpg, png, gif, webp" });

                    var fileName = $"avatar_{user.Id}_{DateTime.UtcNow.Ticks}{extension}";
                    var folderPath = Path.Combine("wwwroot", "avatars");
                    var filePath = Path.Combine(folderPath, fileName);

                    Directory.CreateDirectory(folderPath);

                    // Usuń stary awatar jeśli istnieje
                    if (!string.IsNullOrEmpty(user.AvatarUrl) && user.AvatarUrl.StartsWith("/avatars/"))
                    {
                        var oldFilePath = Path.Combine("wwwroot", user.AvatarUrl.TrimStart('/'));
                        if (System.IO.File.Exists(oldFilePath))
                        {
                            try { System.IO.File.Delete(oldFilePath); }
                            catch { /* Ignoruj błąd usuwania */ }
                        }
                    }

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    user.AvatarUrl = $"/avatars/{fileName}";
                    System.Diagnostics.Debug.WriteLine($"📸 Avatar zagrany: {user.AvatarUrl}");
                }
                else
                {
                    return BadRequest(new { error = "Brak pliku" });
                }

                _context.Users.Update(user);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "Awatar zaktualizowany",
                    avatarUrl = user.AvatarUrl
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Błąd avatara: {ex.Message}");
                return StatusCode(500, new { error = $"Błąd: {ex.Message}" });
            }
        }

        // POST: api/users/avatar/{id}
        [Authorize]
        [HttpPost("avatar/{id}")]
        public async Task<IActionResult> UploadAvatar(int id, IFormFile avatar)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                    return NotFound(new { error = "Użytkownik nie znaleziony" });

                if (avatar == null || avatar.Length == 0)
                    return BadRequest(new { error = "Brak pliku" });

                if (avatar.Length > 200 * 1024)
                    return BadRequest(new { error = "Plik za duży (max 200KB)" });

                var extension = Path.GetExtension(avatar.FileName);
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                if (!allowedExtensions.Contains(extension.ToLower()))
                    return BadRequest(new { error = "Nieobsługiwany format" });

                var fileName = $"avatar_{user.Id}{extension}";
                var filePath = Path.Combine("wwwroot", "avatars", fileName);

                Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await avatar.CopyToAsync(stream);
                }

                user.AvatarUrl = $"/avatars/{fileName}";
                _context.Users.Update(user);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Awatar przesłany", avatarUrl = user.AvatarUrl });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // POST: api/users/reset-password
        [AllowAnonymous]
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.NewPassword))
                    return BadRequest(new { error = "Email i nowe hasło są wymagane" });

                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
                if (user == null)
                    return NotFound(new { error = "Użytkownik nie znaleziony" });

                user.PasswordHash = request.NewPassword;
                _context.Users.Update(user);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Hasło zmienione" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // POST: api/users/change-password
        [Authorize]
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.OldPassword) || 
                    string.IsNullOrWhiteSpace(request.NewPassword) ||
                    string.IsNullOrWhiteSpace(request.ConfirmPassword))
                    return BadRequest(new { error = "Wszystkie pola są wymagane" });

                if (request.NewPassword != request.ConfirmPassword)
                    return BadRequest(new { error = "Nowe hasła nie pasują do siebie" });

                if (request.NewPassword.Length < 6)
                    return BadRequest(new { error = "Hasło musi zawierać co najmniej 6 znaków" });

                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(userIdClaim, out var userId))
                    return Unauthorized(new { error = "Użytkownik nie zalogowany" });

                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                    return NotFound(new { error = "Użytkownik nie znaleziony" });

                if (user.PasswordHash != request.OldPassword)
                    return BadRequest(new { error = "Stare hasło jest nieprawidłowe" });

                user.PasswordHash = request.NewPassword;
                _context.Users.Update(user);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Hasło zostało zmienione" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // GET: api/users/rank/{id}
        [AllowAnonymous]
        [HttpGet("rank/{id}")]
        public async Task<IActionResult> GetUserRank(int id)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.CurrentRank)
                    .FirstOrDefaultAsync(u => u.Id == id);

                if (user == null)
                    return NotFound(new { error = "Użytkownik nie znaleziony" });

                var ranks = await _context.UserRanks.OrderBy(r => r.MinMessages).ToListAsync();
                var newRank = ranks.LastOrDefault(r => r.MinMessages <= user.PostCount);

                if (newRank != null && user.CurrentRankId != newRank.Id)
                {
                    user.CurrentRankId = newRank.Id;
                    _context.Users.Update(user);
                    await _context.SaveChangesAsync();
                }

                return Ok(user.CurrentRank);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // GET: api/users/ranks - Pobierz wszystkie rangi
        [AllowAnonymous]
        [HttpGet("ranks")]
        public async Task<IActionResult> GetAllRanks()
        {
            try
            {
                var ranks = await _context.UserRanks
                    .OrderBy(r => r.MinMessages)
                    .Select(r => new
                    {
                        r.Id,
                        r.Name,
                        r.MinMessages,
                        r.Color,
                        r.Icon
                    })
                    .ToListAsync();

                return Ok(ranks);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }

    // DTOs
    public class UpdateUserRequest
    {
        public string? Bio { get; set; }
        public int? MessagesPerPage { get; set; }
        public int? ThreadsPerPage { get; set; }
        public int? AutoLogoutMinutes { get; set; }
    }

    public class ResetPasswordRequest
    {
        public string Email { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }

    public class UpdateAvatarRequest
    {
        public string Avatar { get; set; } = string.Empty;
    }

    public class ChangePasswordRequest
    {
        public string OldPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}