using Microsoft.AspNetCore.Mvc;
using Npgsql;
using ForumDyskusyjne.Data;

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
        Console.WriteLine($"🔐 Próba logowania: {request.Username}");
        
        try
        {
            // Podstawowa walidacja
            if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password))
            {
                return BadRequest(new { message = "Nazwa użytkownika i hasło są wymagane" });
            }
            
            // Sprawdzenie użytkownika w bazie danych
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            
            var query = "SELECT id, username, password_hash, role, avatar_url, last_activity_at FROM \"user\" WHERE username = @username";
            using var command = new NpgsqlCommand(query, connection);
            command.Parameters.AddWithValue("@username", request.Username);
            
            using var reader = await command.ExecuteReaderAsync();
            
            if (await reader.ReadAsync())
            {
                var userId = reader.GetInt32(0); // id
                var storedUsername = reader.GetString(1); // username
                var passwordHash = reader.GetString(2); // password_hash
                var userRole = reader.GetString(3); // role
                var avatarUrl = reader.IsDBNull(4) ? null : reader.GetString(4); // avatar_url
                var lastActivity = reader.IsDBNull(5) ? (DateTime?)null : reader.GetDateTime(5); // last_activity_at
                
                // TODO: Implementacja właściwego hashowania hasła (BCrypt)
                // Na razie porównanie prostego tekstu (NIEBEZPIECZNE w produkcji!)
                if (passwordHash == request.Password)
                {
                    Console.WriteLine($"✅ Logowanie udane dla: {storedUsername} (ID: {userId}, Rola: {userRole})");
                    
                    // Aktualizuj ostatnią aktywność
                    await reader.CloseAsync();
                    var updateQuery = "UPDATE \"user\" SET last_activity_at = @now WHERE id = @userId";
                    using var updateCommand = new NpgsqlCommand(updateQuery, connection);
                    updateCommand.Parameters.AddWithValue("@now", DateTime.Now);
                    updateCommand.Parameters.AddWithValue("@userId", userId);
                    await updateCommand.ExecuteNonQueryAsync();
                    
                    // Ustaw cookie sesji (w produkcji użyj JWT lub bezpieczniejszej sesji)
                    var sessionData = $"{userId}|{storedUsername}|{userRole}|{avatarUrl ?? ""}";
                    var cookieOptions = new CookieOptions
                    {
                        HttpOnly = false, // Pozwól JavaScript na odczyt cookie dla auth UI
                        SameSite = SameSiteMode.Lax,
                        Expires = request.RememberMe ? DateTime.Now.AddDays(30) : DateTime.Now.AddHours(8)
                    };
                    HttpContext.Response.Cookies.Append("user_session", sessionData, cookieOptions);
                    
                    return Ok(new { 
                        success = true, 
                        message = "Logowanie udane",
                        user = new { 
                            id = userId,
                            username = storedUsername,
                            role = userRole,
                            avatar = avatarUrl
                        }
                    });
                }
                else
                {
                    Console.WriteLine("❌ Nieprawidłowe hasło");
                    return Unauthorized(new { message = "Nieprawidłowe dane logowania" });
                }
            }
            else
            {
                Console.WriteLine($"❌ Użytkownik '{request.Username}' nie został znaleziony");
                return Unauthorized(new { message = "Nieprawidłowe dane logowania" });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Błąd podczas logowania: {ex.Message}");
            return Problem($"Błąd serwera: {ex.Message}");
        }
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        Console.WriteLine($"📝 Próba rejestracji: {request.Username}");
        
        try
        {
            // Walidacja podstawowa
            if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
            {
                return BadRequest(new { message = "Wszystkie pola są wymagane" });
            }
            
            if (request.Username.Length < 3)
            {
                return BadRequest(new { message = "Nazwa użytkownika musi mieć co najmniej 3 znaki" });
            }
            
            if (request.Password.Length < 6)
            {
                return BadRequest(new { message = "Hasło musi mieć co najmniej 6 znaków" });
            }
            
            if (request.Password != request.ConfirmPassword)
            {
                return BadRequest(new { message = "Hasła nie są identyczne" });
            }
            
            if (!request.Terms)
            {
                return BadRequest(new { message = "Musisz zaakceptować regulamin" });
            }
            
            // Sprawdź czy użytkownik już istnieje
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            
            var checkQuery = "SELECT COUNT(*) FROM \"user\" WHERE username = @username OR email = @email";
            using var checkCommand = new NpgsqlCommand(checkQuery, connection);
            checkCommand.Parameters.AddWithValue("@username", request.Username);
            checkCommand.Parameters.AddWithValue("@email", request.Email);
            
            var existingCount = (long)(await checkCommand.ExecuteScalarAsync() ?? 0L);
            if (existingCount > 0)
            {
                return BadRequest(new { message = "Użytkownik z tą nazwą lub e-mailem już istnieje" });
            }
            
            // Utwórz nowego użytkownika
            var insertQuery = @"
                INSERT INTO ""user"" (username, email, password_hash, role, email_verified, is_banned, login_attempts, post_count, auto_logout_minutes, messages_per_page, threads_per_page, created_at, last_activity_at) 
                VALUES (@username, @email, @passwordHash, @role, @emailVerified, @isBanned, @loginAttempts, @postCount, @autoLogout, @messagesPerPage, @threadsPerPage, @createdAt, @lastActivity) 
                RETURNING id";
            
            using var insertCommand = new NpgsqlCommand(insertQuery, connection);
            insertCommand.Parameters.AddWithValue("@username", request.Username);
            insertCommand.Parameters.AddWithValue("@email", request.Email);
            insertCommand.Parameters.AddWithValue("@passwordHash", request.Password); // TODO: Hash password properly
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
            return Problem($"Błąd serwera: {ex.Message}");
        }
    }

    [HttpGet("status")]
    public async Task<IActionResult> Status()
    {
        try
        {
            Console.WriteLine($"🔍 Sprawdzanie statusu uwierzytelniania - cookies count: {HttpContext.Request.Cookies.Count}");
            
            // TODO: Sprawdź sesję/token z cookies
            // Na razie zwracamy przykładowe dane jeśli jest ustawiony cookie
            if (HttpContext.Request.Cookies.ContainsKey("user_session"))
            {
                var sessionValue = HttpContext.Request.Cookies["user_session"];
                Console.WriteLine($"🍪 Znaleziono cookie user_session: {sessionValue}");
                
                // W rzeczywistej aplikacji tutaj sprawdzilibyśmy sesję w bazie
                // Na razie dekodujemy podstawowe informacje z cookie
                if (!string.IsNullOrEmpty(sessionValue))
                {
                    try
                    {
                        // Podstawowa deserializacja (w produkcji użyj JWT lub sesji w bazie)
                        var parts = sessionValue.Split('|');
                        Console.WriteLine($"🔍 Parts count: {parts.Length}, Parts: {string.Join(", ", parts)}");
                        if (parts.Length >= 2)
                        {
                            var userData = new {
                                id = int.Parse(parts[0]),
                                username = parts[1],
                                role = parts.Length > 2 ? parts[2] : "User",
                                avatar = parts.Length > 3 && !string.IsNullOrEmpty(parts[3]) ? parts[3] : null
                            };
                            Console.WriteLine($"✅ Zwracam dane użytkownika: {userData.username}");
                            return Ok(userData);
                        }
                    }
                    catch
                    {
                        // Cookie nieprawidłowy, usuń go
                        HttpContext.Response.Cookies.Delete("user_session");
                    }
                }
            }
            
            Console.WriteLine("❌ Brak cookie user_session lub jest pusty");
            return Unauthorized(new { message = "Not authenticated" });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Błąd sprawdzania statusu uwierzytelniania: {ex.Message}");
            return Problem($"Błąd serwera: {ex.Message}");
        }
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        try
        {
            // Usuń cookie sesji
            HttpContext.Response.Cookies.Delete("user_session");
            
            Console.WriteLine("🚪 Użytkownik wylogowany");
            return Ok(new { success = true, message = "Wylogowano pomyślnie" });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Błąd podczas wylogowania: {ex.Message}");
            return Problem($"Błąd serwera: {ex.Message}");
        }
    }
}

// DTOs dla żądań
public record LoginRequest(string Username, string Password, bool RememberMe);
public record RegisterRequest(string Username, string Email, string Password, string ConfirmPassword, bool Terms);
