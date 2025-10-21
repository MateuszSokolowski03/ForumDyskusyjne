using Microsoft.Extensions.Configuration;
using Npgsql;

// Konfiguracja
var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .Build();

// Pobranie connection stringa
var connectionString = configuration.GetConnectionString("DefaultConnection");

Console.WriteLine("Forum Dyskusyjne");
Console.WriteLine($"Connection string loaded: {!string.IsNullOrEmpty(connectionString)}");

// Przykład testowego połączenia z bazą danych
try
{
    using var connection = new NpgsqlConnection(connectionString);
    await connection.OpenAsync();
    Console.WriteLine("✅ Połączenie z bazą danych udane!");
    
    // Przykładowe zapytanie
    using var command = new NpgsqlCommand("SELECT version()", connection);
    var version = await command.ExecuteScalarAsync();
    Console.WriteLine($"PostgreSQL version: {version}");
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Błąd połączenia z bazą danych: {ex.Message}");
}
