using Microsoft.EntityFrameworkCore;
using ForumDyskusyjne.Data;
using ForumDyskusyjne.Models;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

Console.WriteLine("🚀 Forum Dyskusyjne - Aplikacja Web");

// Konfiguracja connection string
var connectionString = Environment.GetEnvironmentVariable("FORUM_CONNECTION_STRING") ?? 
    builder.Configuration.GetConnectionString("DefaultConnection") ?? 
    "Host=localhost;Port=5432;Database=forum_db;Username=forum_user;Password=forum_password;";

Console.WriteLine($"Connection string loaded: {!string.IsNullOrEmpty(connectionString)}");

// Konfiguracja Entity Framework z PostgreSQL
builder.Services.AddDbContext<ForumDbContext>(options =>
    options.UseNpgsql(connectionString));

// Dodaj kontrolery API i MVC
builder.Services.AddControllers();
builder.Services.AddControllersWithViews(); // Dla formularzy CRUD

// Konfiguracja sesji
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Name = "ForumSession";
});

var app = builder.Build();

// Test połączenia z bazą danych
try
{
    using var connection = new NpgsqlConnection(connectionString);
    await connection.OpenAsync();
    
    using var command = new NpgsqlCommand("SELECT version();", connection);
    var version = await command.ExecuteScalarAsync();
    
    Console.WriteLine("✅ Połączenie z bazą danych udane!");
    Console.WriteLine($"PostgreSQL version: {version}");
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Błąd połączenia z bazą danych: {ex.Message}");
}

// Konfiguracja middleware
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();

// Mapowanie kontrolerów
app.MapControllers(); // API kontrolery

// Routing MVC dla formularzy CRUD  
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Categories}/{action=Index}/{id?}");

// Seed przykładowych danych (jeśli pusta baza)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ForumDbContext>();
    
    // Seed kategorii
    if (!db.Categories.Any())
    {
        db.Categories.AddRange(
            new Category { Name = "Ogólne", Description = "Dyskusje ogólne o wszystkim", Icon = "fa-comments", SortOrder = 1, CreatedAt = DateTime.UtcNow },
            new Category { Name = "Pomoc Techniczna", Description = "Pytania i pomoc dotycząca problemów technicznych", Icon = "fa-life-ring", SortOrder = 2, CreatedAt = DateTime.UtcNow },
            new Category { Name = "Zgłoszenia Błędów", Description = "Raportowanie błędów i problemów", Icon = "fa-bug", SortOrder = 3, CreatedAt = DateTime.UtcNow },
            new Category { Name = "Sugestie", Description = "Pomysły na ulepszenia i nowe funkcje", Icon = "fa-lightbulb", SortOrder = 4, CreatedAt = DateTime.UtcNow },
            new Category { Name = "Off-Topic", Description = "Tematy niezwiązane z główną tematyką", Icon = "fa-coffee", SortOrder = 5, CreatedAt = DateTime.UtcNow }
        );
        db.SaveChanges();
        Console.WriteLine("✅ Dodano przykładowe kategorie");
    }
    
    // Seed forów (jeśli kategorie istnieją)
    if (!db.Forums.Any() && db.Categories.Any())
    {
        var categories = db.Categories.ToList();
        db.Forums.AddRange(
            new Forum { Name = "Dyskusje Ogólne", Description = "Miejsce na każdy temat", CategoryId = categories.First(c => c.Name == "Ogólne").Id, CreatedAt = DateTime.UtcNow },
            new Forum { Name = "Prezentacje", Description = "Przedstaw się społeczności", CategoryId = categories.First(c => c.Name == "Ogólne").Id, CreatedAt = DateTime.UtcNow },
            new Forum { Name = "Problemy Techniczne", Description = "Rozwiązywanie problemów", CategoryId = categories.First(c => c.Name == "Pomoc Techniczna").Id, CreatedAt = DateTime.UtcNow },
            new Forum { Name = "Tutoriale", Description = "Poradniki i instrukcje", CategoryId = categories.First(c => c.Name == "Pomoc Techniczna").Id, CreatedAt = DateTime.UtcNow },
            new Forum { Name = "Znalezione Błędy", Description = "Zgłaszanie wykrytych błędów", CategoryId = categories.First(c => c.Name == "Zgłoszenia Błędów").Id, CreatedAt = DateTime.UtcNow }
        );
        db.SaveChanges();
        Console.WriteLine("✅ Dodano przykładowe fora");
    }
    
    // Seed rang użytkowników
    if (!db.UserRanks.Any())
    {
        db.UserRanks.AddRange(
            new UserRank { Name = "Nowicjusz", MinMessages = 0, Color = "#6c757d", CreatedAt = DateTime.UtcNow },
            new UserRank { Name = "Członek", MinMessages = 10, Color = "#0d6efd", CreatedAt = DateTime.UtcNow },
            new UserRank { Name = "Aktywny Użytkownik", MinMessages = 50, Color = "#198754", CreatedAt = DateTime.UtcNow },
            new UserRank { Name = "Ekspert", MinMessages = 100, Color = "#ffc107", CreatedAt = DateTime.UtcNow },
            new UserRank { Name = "Moderator", MinMessages = 0, Color = "#fd7e14", CanBeSetManually = true, CreatedAt = DateTime.UtcNow },
            new UserRank { Name = "Administrator", MinMessages = 0, Color = "#dc3545", CanBeSetManually = true, CreatedAt = DateTime.UtcNow }
        );
        db.SaveChanges();
        Console.WriteLine("✅ Dodano rangi użytkowników");
    }
    
    // Seed przykładowych użytkowników
    if (!db.Users.Any() && db.UserRanks.Any())
    {
        var ranks = db.UserRanks.ToList();
        var adminRank = ranks.First(r => r.Name == "Administrator");
        var modRank = ranks.First(r => r.Name == "Moderator");
        var userRank = ranks.First(r => r.Name == "Członek");
        
        db.Users.AddRange(
            new User { 
                Username = "admin", 
                Email = "admin@forum.pl", 
                PasswordHash = "dummy_hash", // W produkcji użyj prawdziwego hash
                Role = UserRole.Admin, 
                CurrentRankId = adminRank.Id,
                EmailVerified = true,
                CreatedAt = DateTime.UtcNow 
            },
            new User { 
                Username = "moderator", 
                Email = "mod@forum.pl", 
                PasswordHash = "dummy_hash",
                Role = UserRole.Moderator, 
                CurrentRankId = modRank.Id,
                EmailVerified = true,
                CreatedAt = DateTime.UtcNow 
            },
            new User { 
                Username = "testuser", 
                Email = "user@forum.pl", 
                PasswordHash = "dummy_hash",
                Role = UserRole.User, 
                CurrentRankId = userRank.Id,
                EmailVerified = true,
                CreatedAt = DateTime.UtcNow 
            }
        );
        db.SaveChanges();
        Console.WriteLine("✅ Dodano przykładowych użytkowników");
    }
}

// Przekierowania do statycznych stron HTML
app.MapGet("/", () => Results.Redirect("/forum.html"));
app.MapGet("/login", () => Results.Redirect("/login.html"));
app.MapGet("/register", () => Results.Redirect("/register.html"));
app.MapGet("/admin", () => Results.Redirect("/admin/index.html"));

// Wyświetl dostępne endpointy
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
Console.WriteLine("📋 CRUD Forms: http://localhost:5000/Categories (17 modeli)");

app.Run();