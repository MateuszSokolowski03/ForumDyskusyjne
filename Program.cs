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

Console.WriteLine("🌐 Aplikacja dostępna na:");
Console.WriteLine("   - http://localhost:5000");
Console.WriteLine("   - https://localhost:5001");
Console.WriteLine("📊 Status API: http://localhost:5000/api/status");

app.Run();
