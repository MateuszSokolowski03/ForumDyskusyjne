using Npgsql;
using Microsoft.EntityFrameworkCore;
using ForumDyskusyjne.Data;

var builder = WebApplication.CreateBuilder(args);

// Konfiguracja Entity Framework Core
builder.Services.AddDbContext<ForumDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Dodaj usługi
builder.Services.AddControllers();

var app = builder.Build();

// Konfiguruj middleware
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

// Obsługa plików statycznych (CSS, JS, obrazy)
app.UseStaticFiles();

// Routing
app.UseRouting();

// Test połączenia z bazą danych przy starcie
var connectionString = app.Configuration.GetConnectionString("DefaultConnection");
Console.WriteLine("🚀 Forum Dyskusyjne - Aplikacja Web");
Console.WriteLine($"Connection string loaded: {!string.IsNullOrEmpty(connectionString)}");

try
{
    using var connection = new NpgsqlConnection(connectionString);
    await connection.OpenAsync();
    Console.WriteLine("✅ Połączenie z bazą danych udane!");
    
    using var command = new NpgsqlCommand("SELECT version()", connection);
    var version = await command.ExecuteScalarAsync();
    Console.WriteLine($"PostgreSQL version: {version}");
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Błąd połączenia z bazą danych: {ex.Message}");
}

// Główna strona forum - przekierowanie na index.html
app.MapGet("/", () => Results.Redirect("/index.html"));

// Strona logowania
app.MapGet("/login", () => Results.Redirect("/login.html"));

// Strona rejestracji
app.MapGet("/register", () => Results.Redirect("/register.html"));

// Widok forum


// Panel administracyjny - serwowanie bezpośrednie bez przekierowań
app.MapGet("/admin", async (HttpContext context) =>
{
    var filePath = Path.Combine(app.Environment.WebRootPath, "admin", "index.html");
    if (File.Exists(filePath))
    {
        context.Response.ContentType = "text/html";
        await context.Response.SendFileAsync(filePath);
        return Results.Empty;
    }
    return Results.NotFound();
});

app.MapGet("/admin/{page}", async (string page, HttpContext context) =>
{
    // Zabezpieczenie przed path traversal
    if (page.Contains("..") || page.Contains("/") || page.Contains("\\"))
    {
        return Results.BadRequest("Invalid page name");
    }
    
    var filePath = Path.Combine(app.Environment.WebRootPath, "admin", $"{page}.html");
    if (File.Exists(filePath))
    {
        context.Response.ContentType = "text/html";
        await context.Response.SendFileAsync(filePath);
        return Results.Empty;
    }
    return Results.NotFound($"Admin page '{page}' not found");
});

// API endpoint dla sprawdzenia statusu
app.MapGet("/api/status", async () =>
{
    try
    {
        using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        
        using var command = new NpgsqlCommand("SELECT version()", connection);
        var version = await command.ExecuteScalarAsync();
        
        return Results.Ok(new { 
            status = "OK", 
            database = "Connected", 
            version = version?.ToString(),
            timestamp = DateTime.Now 
        });
    }
    catch (Exception ex)
    {
        return Results.Problem($"Database error: {ex.Message}");
    }
});

// API endpoint dla logowania (podstawowa wersja)
app.MapPost("/api/auth/login", async (HttpContext context, LoginRequest request) =>
{
    Console.WriteLine($"🔐 Próba logowania: {request.Username}");
    
    try
    {
        // TODO: Sprawdzenie w bazie danych
        // Na razie podstawowa walidacja
        if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password))
        {
            return Results.BadRequest(new { message = "Nazwa użytkownika i hasło są wymagane" });
        }
        
        // Sprawdzenie użytkownika w bazie danych
        using var connection = new NpgsqlConnection(connectionString);
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
                context.Response.Cookies.Append("user_session", sessionData, cookieOptions);
                
                return Results.Ok(new { 
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
                return Results.Json(new { message = "Nieprawidłowe dane logowania" }, statusCode: 401);
            }
        }
        else
        {
            Console.WriteLine($"❌ Użytkownik '{request.Username}' nie został znaleziony");
            return Results.Json(new { message = "Nieprawidłowe dane logowania" }, statusCode: 401);
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Błąd podczas logowania: {ex.Message}");
        return Results.Problem($"Błąd serwera: {ex.Message}");
    }
});

// API endpoint dla rejestracji
app.MapPost("/api/auth/register", async (RegisterRequest request) =>
{
    Console.WriteLine($"📝 Próba rejestracji: {request.Username}");
    
    try
    {
        // Walidacja podstawowa
        if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
        {
            return Results.BadRequest(new { message = "Wszystkie pola są wymagane" });
        }
        
        if (request.Username.Length < 3)
        {
            return Results.BadRequest(new { message = "Nazwa użytkownika musi mieć co najmniej 3 znaki" });
        }
        
        if (request.Password.Length < 6)
        {
            return Results.BadRequest(new { message = "Hasło musi mieć co najmniej 6 znaków" });
        }
        
        if (request.Password != request.ConfirmPassword)
        {
            return Results.BadRequest(new { message = "Hasła nie są identyczne" });
        }
        
        if (!request.Terms)
        {
            return Results.BadRequest(new { message = "Musisz zaakceptować regulamin" });
        }
        
        // Sprawdź czy użytkownik już istnieje
        using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        
        var checkQuery = "SELECT COUNT(*) FROM \"user\" WHERE username = @username OR email = @email";
        using var checkCommand = new NpgsqlCommand(checkQuery, connection);
        checkCommand.Parameters.AddWithValue("@username", request.Username);
        checkCommand.Parameters.AddWithValue("@email", request.Email);
        
        var existingCount = (long)(await checkCommand.ExecuteScalarAsync() ?? 0L);
        if (existingCount > 0)
        {
            return Results.BadRequest(new { message = "Użytkownik z tą nazwą lub e-mailem już istnieje" });
        }
        
        // Utwórz nowego użytkownika
        var insertQuery = @"
            INSERT INTO ""user"" (username, email, password_hash, role, created_at, last_activity_at) 
            VALUES (@username, @email, @passwordHash, @role, @createdAt, @lastActivity) 
            RETURNING id";
        
        using var insertCommand = new NpgsqlCommand(insertQuery, connection);
        insertCommand.Parameters.AddWithValue("@username", request.Username);
        insertCommand.Parameters.AddWithValue("@email", request.Email);
        insertCommand.Parameters.AddWithValue("@passwordHash", request.Password); // TODO: Hash password properly
        insertCommand.Parameters.AddWithValue("@role", "user");
        insertCommand.Parameters.AddWithValue("@createdAt", DateTime.Now);
        insertCommand.Parameters.AddWithValue("@lastActivity", DateTime.Now);
        
        var newUserId = (int)(await insertCommand.ExecuteScalarAsync() ?? 0);
        
        Console.WriteLine($"✅ Rejestracja udana dla: {request.Username} (ID: {newUserId})");
        
        return Results.Ok(new { 
            success = true, 
            message = "Konto zostało utworzone pomyślnie",
            user = new { 
                id = newUserId,
                username = request.Username,
                email = request.Email,
                role = "user"
            }
        });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Błąd podczas rejestracji: {ex.Message}");
        return Results.Problem($"Błąd serwera: {ex.Message}");
    }
});

// API Admin - Statystyki
app.MapGet("/api/admin/stats", async () =>
{
    try
    {
        using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        
        // Pobierz statystyki z rzeczywistych tabel
        var usersCount = await GetCountAsync(connection, "\"user\"");
        var threadsCount = await GetCountAsync(connection, "thread");
        var messagesCount = await GetCountAsync(connection, "message");
        var categoriesCount = await GetCountAsync(connection, "category");
        
        return Results.Ok(new {
            users = usersCount,
            threads = threadsCount,
            messages = messagesCount,
            categories = categoriesCount
        });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Błąd pobierania statystyk: {ex.Message}");
        return Results.Problem($"Błąd serwera: {ex.Message}");
    }
});

// API Admin - Lista użytkowników
app.MapGet("/api/admin/users", async (int page = 1, int pageSize = 10, string search = "", string role = "", string status = "") =>
{
    try
    {
        using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        
        var offset = (page - 1) * pageSize;
        var whereConditions = new List<string>();
        var parameters = new List<object>();
        
        if (!string.IsNullOrEmpty(search))
        {
            whereConditions.Add($"(username ILIKE @search OR email ILIKE @search)");
            parameters.Add(new { search = $"%{search}%" });
        }
        
        if (!string.IsNullOrEmpty(role))
        {
            whereConditions.Add("role = @role");
            parameters.Add(new { role });
        }
        
        var whereClause = whereConditions.Count > 0 ? "WHERE " + string.Join(" AND ", whereConditions) : "";
        
        var query = $@"
            SELECT id, username, email, role, created_at, last_activity_at, avatar_url
            FROM ""user""
            {whereClause}
            ORDER BY created_at DESC
            LIMIT @pageSize OFFSET @offset";
            
        var countQuery = $@"
            SELECT COUNT(*)
            FROM ""user""
            {whereClause}";
        
        using var command = new NpgsqlCommand(query, connection);
        command.Parameters.AddWithValue("@pageSize", pageSize);
        command.Parameters.AddWithValue("@offset", offset);
        
        if (!string.IsNullOrEmpty(search))
            command.Parameters.AddWithValue("@search", $"%{search}%");
        if (!string.IsNullOrEmpty(role))
            command.Parameters.AddWithValue("@role", role);
        
        var users = new List<object>();
        using var reader = await command.ExecuteReaderAsync();
        
        while (await reader.ReadAsync())
        {
            users.Add(new
            {
                id = reader.GetInt32(0),
                username = reader.GetString(1),
                email = reader.IsDBNull(2) ? null : reader.GetString(2),
                role = reader.GetString(3),
                created_at = reader.GetDateTime(4),
                last_activity_at = reader.IsDBNull(5) ? (DateTime?)null : reader.GetDateTime(5),
                avatar_url = reader.IsDBNull(6) ? null : reader.GetString(6)
            });
        }
        
        reader.Close();
        
        // Get total count
        using var countCommand = new NpgsqlCommand(countQuery, connection);
        if (!string.IsNullOrEmpty(search))
            countCommand.Parameters.AddWithValue("@search", $"%{search}%");
        if (!string.IsNullOrEmpty(role))
            countCommand.Parameters.AddWithValue("@role", role);
            
        var total = (long)(await countCommand.ExecuteScalarAsync() ?? 0L);
        
        return Results.Ok(new { users, total });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Błąd pobierania użytkowników: {ex.Message}");
        return Results.Problem($"Błąd serwera: {ex.Message}");
    }
});

// API Admin - Kategorie
app.MapGet("/api/admin/categories", async () =>
{
    try
    {
        using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        
        var query = @"
            SELECT c.id, c.name, c.description, c.created_at,
                   COUNT(f.id) as forums_count
            FROM category c
            LEFT JOIN forum f ON c.id = f.category_id
            GROUP BY c.id, c.name, c.description, c.created_at
            ORDER BY c.created_at DESC";
        
        using var command = new NpgsqlCommand(query, connection);
        using var reader = await command.ExecuteReaderAsync();
        
        var categories = new List<object>();
        while (await reader.ReadAsync())
        {
            categories.Add(new
            {
                id = reader.GetInt32(0),
                name = reader.GetString(1),
                description = reader.IsDBNull(2) ? null : reader.GetString(2),
                createdAt = reader.GetDateTime(3),
                forumsCount = reader.GetInt64(4)
            });
        }
        
        return Results.Ok(categories);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Błąd pobierania kategorii: {ex.Message}");
        return Results.Problem($"Błąd serwera: {ex.Message}");
    }
});

// API Admin - Fora
app.MapGet("/api/admin/forums", async () =>
{
    try
    {
        using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        
        var query = @"
            SELECT f.id, f.name, f.description, f.category_id, f.created_at,
                   c.name as category_name,
                   COUNT(t.id) as threads_count
            FROM forum f
            JOIN category c ON f.category_id = c.id
            LEFT JOIN thread t ON f.id = t.forum_id
            GROUP BY f.id, f.name, f.description, f.category_id, f.created_at, c.name
            ORDER BY f.created_at DESC";
        
        using var command = new NpgsqlCommand(query, connection);
        using var reader = await command.ExecuteReaderAsync();
        
        var forums = new List<object>();
        while (await reader.ReadAsync())
        {
            forums.Add(new
            {
                id = reader.GetInt32(0),
                name = reader.GetString(1),
                description = reader.IsDBNull(2) ? null : reader.GetString(2),
                categoryId = reader.GetInt32(3),
                categoryName = reader.GetString(5),
                createdAt = reader.GetDateTime(4),
                threadsCount = reader.GetInt64(6)
            });
        }
        
        return Results.Ok(forums);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Błąd pobierania for: {ex.Message}");
        return Results.Problem($"Błąd serwera: {ex.Message}");
    }
});

// API Admin - Wątki
app.MapGet("/api/admin/threads", async () =>
{
    try
    {
        using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        
        var query = @"
            SELECT t.id, t.title, t.author_id, t.forum_id, t.is_pinned, t.views, 
                   t.replies_count, t.created_at,
                   u.username as author_name,
                   f.name as forum_name,
                   c.name as category_name
            FROM thread t
            JOIN ""user"" u ON t.author_id = u.id
            JOIN forum f ON t.forum_id = f.id
            JOIN category c ON f.category_id = c.id
            ORDER BY t.created_at DESC
            LIMIT 50";
        
        using var command = new NpgsqlCommand(query, connection);
        using var reader = await command.ExecuteReaderAsync();
        
        var threads = new List<object>();
        while (await reader.ReadAsync())
        {
            threads.Add(new
            {
                id = reader.GetInt32(0),
                title = reader.GetString(1),
                authorId = reader.GetInt32(2),
                forumId = reader.GetInt32(3),
                isPinned = reader.GetBoolean(4),
                views = reader.GetInt32(5),
                repliesCount = reader.GetInt32(6),
                createdAt = reader.GetDateTime(7),
                authorName = reader.GetString(8),
                forumName = reader.GetString(9),
                categoryName = reader.GetString(10)
            });
        }
        
        return Results.Ok(threads);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Błąd pobierania wątków: {ex.Message}");
        return Results.Problem($"Błąd serwera: {ex.Message}");
    }
});

// API Admin - Słowa zakazane
app.MapGet("/api/admin/banned-words", async () =>
{
    try
    {
        using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        
        var query = "SELECT id, word, created_at FROM banned_word ORDER BY created_at DESC";
        
        using var command = new NpgsqlCommand(query, connection);
        using var reader = await command.ExecuteReaderAsync();
        
        var bannedWords = new List<object>();
        while (await reader.ReadAsync())
        {
            bannedWords.Add(new
            {
                id = reader.GetInt32(0),
                word = reader.GetString(1),
                createdAt = reader.GetDateTime(2)
            });
        }
        
        return Results.Ok(bannedWords);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Błąd pobierania słów zakazanych: {ex.Message}");
        return Results.Problem($"Błąd serwera: {ex.Message}");
    }
});

// API Admin - Dodaj słowo zakazane
app.MapPost("/api/admin/banned-words", async (BannedWordRequest request) =>
{
    try
    {
        using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        
        var query = "INSERT INTO banned_word (word, created_at) VALUES (@word, @createdAt) RETURNING id";
        
        using var command = new NpgsqlCommand(query, connection);
        command.Parameters.AddWithValue("@word", request.Word.ToLower().Trim());
        command.Parameters.AddWithValue("@createdAt", DateTime.Now);
        
        var id = (int)(await command.ExecuteScalarAsync() ?? 0);
        
        return Results.Ok(new { success = true, id, word = request.Word });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Błąd dodawania słowa zakazanego: {ex.Message}");
        return Results.Problem($"Błąd serwera: {ex.Message}");
    }
});

// API Admin - Usuń słowo zakazane
app.MapDelete("/api/admin/banned-words/{id}", async (int id) =>
{
    try
    {
        using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        
        var query = "DELETE FROM banned_word WHERE id = @id";
        
        using var command = new NpgsqlCommand(query, connection);
        command.Parameters.AddWithValue("@id", id);
        
        var affected = await command.ExecuteNonQueryAsync();
        
        return Results.Ok(new { success = affected > 0 });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Błąd usuwania słowa zakazanego: {ex.Message}");
        return Results.Problem($"Błąd serwera: {ex.Message}");
    }
});

// Helper method for counting records
static async Task<long> GetCountAsync(NpgsqlConnection connection, string tableName)
{
    using var command = new NpgsqlCommand($"SELECT COUNT(*) FROM {tableName}", connection);
    return (long)(await command.ExecuteScalarAsync() ?? 0L);
}

// Dodatkowy routing dla panelu administratora (MUSI BYĆ PRZED app.Run())
app.MapGet("/admin/users", () => Results.Redirect("/admin/users.html"));
app.MapGet("/admin/categories", () => Results.Redirect("/admin/categories.html"));
app.MapGet("/admin/threads", () => Results.Redirect("/admin/threads.html"));
app.MapGet("/admin/banned-words", () => Results.Redirect("/admin/banned-words.html"));
app.MapGet("/admin/settings", () => Results.Redirect("/admin/settings.html"));

// API endpoint dla sprawdzenia statusu uwierzytelniania
app.MapGet("/api/auth/status", async (HttpContext context) =>
{
    try
    {
        Console.WriteLine($"🔍 Sprawdzanie statusu uwierzytelniania - cookies count: {context.Request.Cookies.Count}");
        
        // TODO: Sprawdź sesję/token z cookies
        // Na razie zwracamy przykładowe dane jeśli jest ustawiony cookie
        if (context.Request.Cookies.ContainsKey("user_session"))
        {
            var sessionValue = context.Request.Cookies["user_session"];
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
                            role = parts.Length > 2 ? parts[2] : "user",
                            avatar = parts.Length > 3 && !string.IsNullOrEmpty(parts[3]) ? parts[3] : null
                        };
                        Console.WriteLine($"✅ Zwracam dane użytkownika: {userData.username}");
                        return Results.Ok(userData);
                    }
                }
                catch
                {
                    // Cookie nieprawidłowy, usuń go
                    context.Response.Cookies.Delete("user_session");
                }
            }
        }
        
        Console.WriteLine("❌ Brak cookie user_session lub jest pusty");
        return Results.Json(new { message = "Not authenticated" }, statusCode: 401);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Błąd sprawdzania statusu uwierzytelniania: {ex.Message}");
        return Results.Problem($"Błąd serwera: {ex.Message}");
    }
});

// API endpoint dla wylogowania
app.MapPost("/api/auth/logout", async (HttpContext context) =>
{
    try
    {
        // Usuń cookie sesji
        context.Response.Cookies.Delete("user_session");
        
        Console.WriteLine("🚪 Użytkownik wylogowany");
        return Results.Ok(new { success = true, message = "Wylogowano pomyślnie" });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Błąd podczas wylogowania: {ex.Message}");
        return Results.Problem($"Błąd serwera: {ex.Message}");
    }
});

Console.WriteLine("🌐 Aplikacja dostępna na:");
Console.WriteLine("   - http://localhost:5000");
Console.WriteLine("   - https://localhost:5001");

Console.WriteLine("📊 Status API: http://localhost:5000/api/status");
Console.WriteLine("🔐 Login API: http://localhost:5000/api/auth/login");
Console.WriteLine("🔑 Auth Status API: http://localhost:5000/api/auth/status");
Console.WriteLine("🚪 Logout API: http://localhost:5000/api/auth/logout");
Console.WriteLine("📝 Register API: http://localhost:5000/api/auth/register");
Console.WriteLine("👨‍💼 Admin Panel: http://localhost:5000/admin");
Console.WriteLine("📂 Forum: http://localhost:5000/forum.html");

app.Run();

// Modele dla żądań
public record LoginRequest(string Username, string Password, bool RememberMe);
public record RegisterRequest(string Username, string Email, string Password, string ConfirmPassword, bool Terms);
public record BannedWordRequest(string Word);
