using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using ForumDyskusyjne.Data;
using ForumDyskusyjne.Models;
using Npgsql;
using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.Authentication;

var builder = WebApplication.CreateBuilder(args);

int FindAvailablePort(int startPort, int range = 20)
{
    for (int p = startPort; p < startPort + range; p++)
    {
        try
        {
            var listener = new TcpListener(IPAddress.Loopback, p);
            listener.Start();
            listener.Stop();
            return p;
        }
        catch { }
    }
    return -1;
}

int httpPort = 5000;
int httpsPort = 5001;
int chosenHttp = FindAvailablePort(httpPort);
int chosenHttps = FindAvailablePort(httpsPort);

if (chosenHttp == -1) chosenHttp = 0; // let Kestrel pick an ephemeral port if none found
if (chosenHttps == -1) chosenHttps = 0;

if (chosenHttp != httpPort)
    Console.WriteLine($"⚠️ Port {httpPort} zajęty — używam portu {chosenHttp}");

// Bezpiecznie skonfiguruj URLe
var urls = new List<string>();
if (chosenHttp > 0) urls.Add($"http://localhost:{chosenHttp}");
if (chosenHttps > 0) urls.Add($"https://localhost:{chosenHttps}");
if (urls.Count > 0) builder.WebHost.UseUrls(urls.ToArray());

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:5000", "https://localhost:5001")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});


Console.WriteLine("🚀 Forum Dyskusyjne - Aplikacja Web");

var connectionString = Environment.GetEnvironmentVariable("FORUM_CONNECTION_STRING") ?? 
    builder.Configuration.GetConnectionString("DefaultConnection") ?? 
    "Host=localhost;Port=5432;Database=forum_db;Username=forum_user;Password=forum_password;";

Console.WriteLine($"Connection string loaded: {!string.IsNullOrEmpty(connectionString)}");

builder.Services.AddDbContext<ForumDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

// DODANE - Konfiguracja autentykacji
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
    {
        options.Cookie.Name = "ForumAuth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;               // działa na http://localhost
        options.Cookie.SecurePolicy = CookieSecurePolicy.None; // pozwala na http w dev
        options.LoginPath = "/login.html";
        options.LogoutPath = "/logout";
        options.AccessDeniedPath = "/access-denied.html"; // Strona braku dostępu
        options.ExpireTimeSpan = TimeSpan.FromDays(30);
        options.SlidingExpiration = true;
        options.Events = new Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationEvents
        {
            OnRedirectToLogin = ctx =>
            {
                if (ctx.Request.Path.StartsWithSegments("/api") ||
                    ctx.Request.Headers["Accept"].FirstOrDefault()?.Contains("application/json") == true)
                {
                    ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return Task.CompletedTask;
                }
                ctx.Response.Redirect(ctx.RedirectUri);
                return Task.CompletedTask;
            },
            OnRedirectToAccessDenied = ctx =>
            {
                if (ctx.Request.Path.StartsWithSegments("/api") ||
                    ctx.Request.Headers["Accept"].FirstOrDefault()?.Contains("application/json") == true)
                {
                    ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return Task.CompletedTask;
                }
                ctx.Response.Redirect(ctx.RedirectUri);
                return Task.CompletedTask;
            }
        };
    });
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Name = "ForumSession";
});




var app = builder.Build();


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

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseCors("AllowFrontend"); 

app.UseAuthentication();
app.UseAuthorization();

// Middleware do ochrony folderu /admin
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/admin"))
    {
        if (!context.User.IsInRole("Admin"))
        {
            context.Response.Redirect("/access-denied.html");
            return;
        }
    }
    await next.Invoke();
});

// Middleware do ochrony folderu /mod
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/mod"))
    {
        if (!context.User.IsInRole("Moderator") && !context.User.IsInRole("Admin"))
        {
            context.Response.Redirect("/access-denied.html");
            return;
        }
    }
    await next.Invoke();
});

app.UseStaticFiles();

// ===== AUTORYZACJA - MUSI BYĆ PO UseRouting() I PRZED UseSession() =====


// ===== MIDDLEWARE DO AKTUALIZACJI OSTATNIEJ AKTYWNOŚCI =====
app.Use(async (context, next) =>
{
    if (context.User?.Identity?.IsAuthenticated == true)
    {
        var userIdClaim = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(userIdClaim, out var userId))
        {
            using (var scope = context.RequestServices.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ForumDbContext>();
                var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
                
                if (user != null)
                {
                    // Aktualizuj ostatnią aktywność (co 5 minut maksymalnie)
                    if (user.LastActivityAt == null || (DateTime.UtcNow - user.LastActivityAt.Value).TotalMinutes >= 5)
                    {
                        user.LastActivityAt = DateTime.UtcNow;
                        await db.SaveChangesAsync();
                        System.Diagnostics.Debug.WriteLine($"📍 [ACTIVITY] Zaktualizowana ostatnia aktywność dla użytkownika {userId}");
                    }
                }
            }
        }
    }
    
    await next();
});

// ===== MIDDLEWARE DO SPRAWDZENIA ZABLOKOWANIA UŻYTKOWNIKA =====
// Musi być ПОСЛЕ UseAuthorization() aby sprawdzić zalogowanego użytkownika
app.Use(async (context, next) =>
{
    if (context.User?.Identity?.IsAuthenticated == true)
    {
        var userIdClaim = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(userIdClaim, out var userId))
        {
            using (var scope = context.RequestServices.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ForumDbContext>();
                var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
                
                if (user != null && user.IsBanned)
                {
                    System.Diagnostics.Debug.WriteLine($"🚫 [BAN] Użytkownik {userId} ({user.Username}) jest zablokowany - wylogowuję");
                    
                    // Wyloguj zablokowanego użytkownika
                    await context.SignOutAsync(Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme);
                    if (context.Request.Cookies.ContainsKey("user_session"))
                        context.Response.Cookies.Delete("user_session");
                    
                    // Jeśli to żądanie API, zwróć 403
                    if (context.Request.Path.StartsWithSegments("/api") ||
                        context.Request.Headers["Accept"].FirstOrDefault()?.Contains("application/json") == true)
                    {
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        await context.Response.WriteAsJsonAsync(new { error = "Twoje konto zostało zablokowane" });
                        return;
                    }
                    
                    // Dla żądań HTML, redirect na login
                    context.Response.Redirect("/login");
                    return;
                }
            }
        }
    }
    
    await next();
});

app.UseSession();

// ===== MAPOWANIE TRAS =====
app.MapControllers();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ForumDbContext>();
    
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
                PasswordHash = "dummy_hash",
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
        app.MapGet("/api/forum/debug", async (ForumDbContext db) =>
    {
        try
        {
            var categoriesCount = await db.Categories.CountAsync();
            var forumsCount = await db.Forums.CountAsync();
            return Results.Ok(new { ok = true, categoriesCount, forumsCount });
        }
        catch (Exception ex)
        {
            return Results.Problem(title: "Db error", detail: ex.Message);
        }
    });

}


Console.WriteLine("🌐 Aplikacja dostępna na:");
if (chosenHttp != 0)
    Console.WriteLine($"   - http://localhost:{chosenHttp}");
if (chosenHttps != 0)
    Console.WriteLine($"   - https://localhost:{chosenHttps}");
var baseHttp = chosenHttp != 0 ? $"http://localhost:{chosenHttp}" : "http://localhost";
Console.WriteLine($"📊 Status API: {baseHttp}/api/status");
Console.WriteLine($"🔐 Login API: {baseHttp}/api/auth/login");
Console.WriteLine($"🔑 Auth Status API: {baseHttp}/api/auth/status");
Console.WriteLine($"🚪 Logout API: {baseHttp}/api/auth/logout");
Console.WriteLine($"📝 Register API: {baseHttp}/api/auth/register");
Console.WriteLine($"👨‍💼 Admin Panel: {baseHttp}/admin");
Console.WriteLine($"📂 Forum: {baseHttp}/forum.html");
Console.WriteLine($"📋 CRUD Forms: {baseHttp}/Categories (i inne kontrolery)");

app.Run();