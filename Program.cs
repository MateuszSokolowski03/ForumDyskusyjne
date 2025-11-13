using Npgsql;

var builder = WebApplication.CreateBuilder(args);

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
app.MapPost("/api/auth/login", async (LoginRequest request) =>
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
                
                return Results.Ok(new { 
                    success = true, 
                    message = "Logowanie udane",
                    user = new { 
                        id = userId,
                        username = storedUsername,
                        role = userRole,
                        avatar_url = avatarUrl,
                        last_activity = lastActivity
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

Console.WriteLine("🌐 Aplikacja dostępna na:");
Console.WriteLine("   - http://localhost:5000");
Console.WriteLine("   - https://localhost:5001");
Console.WriteLine("📊 Status API: http://localhost:5000/api/status");
Console.WriteLine("🔐 Login API: http://localhost:5000/api/auth/login");
Console.WriteLine("📝 Register API: http://localhost:5000/api/auth/register");

app.Run();

// Modele dla żądań
public record LoginRequest(string Username, string Password, bool RememberMe);
public record RegisterRequest(string Username, string Email, string Password, string ConfirmPassword, bool Terms);
