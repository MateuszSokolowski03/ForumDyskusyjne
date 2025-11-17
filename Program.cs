using Microsoft.EntityFrameworkCore;
using ForumDyskusyjne.Data;
using Npgsql;

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

// Mapowanie Controllers - wszystkie endpointy są w kontrolerach
app.MapControllers();

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
