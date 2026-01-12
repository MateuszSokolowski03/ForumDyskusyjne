using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using ForumDyskusyjne.Data;
using ForumDyskusyjne.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using System.Collections.Generic;

namespace ForumDyskusyjne.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly ForumDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly string _connectionString;

    public AuthController(ForumDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
        _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
                return BadRequest(new { message = "Nazwa użytkownika i hasło są wymagane" });

            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Username == request.Username || u.Email == request.Username);

            if (user == null)
                return Unauthorized(new { message = "Nieprawidłowe dane logowania" });

            if (user.IsBanned)
                return Unauthorized(new { message = "Konto zostało zablokowane" });

            if (user.PasswordHash != request.Password)
                return Unauthorized(new { message = "Nieprawidłowe dane logowania" });

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
                new Claim(ClaimTypes.Role, user.Role.ToString())
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            var props = new AuthenticationProperties
            {
                IsPersistent = request.RememberMe,
                AllowRefresh = true
            };

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, props);

            var sessionVal = $"{user.Id}|{user.Username}|{user.Role}|{user.AvatarUrl ?? ""}";
            var cookieOptions = new CookieOptions
            {
                HttpOnly = false,
                Secure = Request.IsHttps,
                SameSite = SameSiteMode.Lax,
                Expires = request.RememberMe ? DateTimeOffset.UtcNow.AddDays(30) : null
            };
            Response.Cookies.Append("user_session", sessionVal, cookieOptions);

            return Ok(new
            {
                user = new
                {
                    id = user.Id,
                    username = user.Username,
                    email = user.Email,
                    role = user.Role,
                    avatar = user.AvatarUrl
                }
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        Console.WriteLine($"📝 Próba rejestracji: {request.Username}");
        
        try
        {
            if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
                return BadRequest(new { message = "Wszystkie pola są wymagane" });
            
            if (request.Username.Length < 3)
                return BadRequest(new { message = "Nazwa użytkownika musi mieć co najmniej 3 znaki" });
            
            if (request.Password.Length < 6)
                return BadRequest(new { message = "Hasło musi mieć co najmniej 6 znaków" });
            
            if (request.Password != request.ConfirmPassword)
                return BadRequest(new { message = "Hasła nie są identyczne" });
            
            if (!request.Terms)
                return BadRequest(new { message = "Musisz zaakceptować regulamin" });
            
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            
            var checkQuery = "SELECT COUNT(*) FROM \"user\" WHERE username = @username OR email = @email";
            using var checkCommand = new NpgsqlCommand(checkQuery, connection);
            checkCommand.Parameters.AddWithValue("@username", request.Username);
            checkCommand.Parameters.AddWithValue("@email", request.Email);
            
            var existingCount = (long)(await checkCommand.ExecuteScalarAsync() ?? 0L);
            if (existingCount > 0)
                return BadRequest(new { message = "Użytkownik z tą nazwą lub e-mailem już istnieje" });
            
            var insertQuery = @"
                INSERT INTO ""user"" (username, email, password_hash, role, email_verified, is_banned, login_attempts, post_count, auto_logout_minutes, messages_per_page, threads_per_page, created_at, last_activity_at) 
                VALUES (@username, @email, @passwordHash, @role, @emailVerified, @isBanned, @loginAttempts, @postCount, @autoLogout, @messagesPerPage, @threadsPerPage, @createdAt, @lastActivity) 
                RETURNING id";
            
            using var insertCommand = new NpgsqlCommand(insertQuery, connection);
            insertCommand.Parameters.AddWithValue("@username", request.Username);
            insertCommand.Parameters.AddWithValue("@email", request.Email);
            insertCommand.Parameters.AddWithValue("@passwordHash", request.Password);
            insertCommand.Parameters.AddWithValue("@role", "User");
            insertCommand.Parameters.AddWithValue("@emailVerified", false);
            insertCommand.Parameters.AddWithValue("@isBanned", false);
            insertCommand.Parameters.AddWithValue("@loginAttempts", 0);
            insertCommand.Parameters.AddWithValue("@postCount", 0);
            insertCommand.Parameters.AddWithValue("@autoLogout", 30);
            insertCommand.Parameters.AddWithValue("@messagesPerPage", 20);
            insertCommand.Parameters.AddWithValue("@threadsPerPage", 15);
            insertCommand.Parameters.AddWithValue("@createdAt", DateTime.UtcNow);
            insertCommand.Parameters.AddWithValue("@lastActivity", DateTime.UtcNow);
            
            var newUserId = (int)(await insertCommand.ExecuteScalarAsync() ?? 0);
            
            Console.WriteLine($"✅ Rejestracja udana dla: {request.Username} (ID: {newUserId})");
            
            return Ok(new { 
                success = true, 
                message = "Konto zostało utworzone pomyślnie",
                user = new { 
                    id = newUserId,
                    username = request.Username,
                    email = request.Email,
                    role = "User"
                }
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Błąd podczas rejestracji: {ex.Message}");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("status")]
    public async Task<IActionResult> Status()
    {
        if (User?.Identity?.IsAuthenticated == true)
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var name = User.Identity?.Name ?? User.FindFirst(ClaimTypes.Name)?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            var email = User.FindFirst(ClaimTypes.Email)?.Value;

            // Sprawdź czy użytkownik nie jest zablokowany
            if (int.TryParse(idClaim, out var uid))
            {
                var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == uid);
                if (user?.IsBanned == true)
                {
                    // Użytkownik jest zablokowany - zwróć 403
                    return StatusCode(StatusCodes.Status403Forbidden, new { message = "Twoje konto zostało zablokowane" });
                }
            }

            // Dla moderatorów, dołącz listę przypisanych forum
            List<int>? moderatedForumIds = null;
            if (int.TryParse(idClaim, out var uid2) && role == UserRole.Moderator.ToString())
            {
                moderatedForumIds = await _context.ForumModerators
                    .Where(fm => fm.UserId == uid2)
                    .Select(fm => fm.ForumId)
                    .ToListAsync();
                
                System.Diagnostics.Debug.WriteLine($"🔍 [AUTH] Moderator {uid2} has forums: {string.Join(", ", moderatedForumIds ?? new List<int>())}");
            }

            System.Diagnostics.Debug.WriteLine($"🔍 [AUTH] Status returned - role: {role}, moderatedForumIds: {(moderatedForumIds != null ? string.Join(", ", moderatedForumIds) : "null")}");

            return Ok(new
            {
                id = idClaim,
                username = name,
                email = email,
                role = role,
                moderatedForumIds = moderatedForumIds
            });
        }

        return Unauthorized(new { message = "Brak zalogowanego użytkownika" });
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        if (Request.Cookies.ContainsKey("user_session"))
            Response.Cookies.Delete("user_session");
        return Ok(new { message = "Wylogowano" });
    }
}

// DTOs
public record LoginRequest(string Username, string Password, bool RememberMe);
public record RegisterRequest(string Username, string Email, string Password, string ConfirmPassword, bool Terms);