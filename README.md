# Forum Dyskusyjne

## 📋 Wymagania

- .NET 8.0 SDK
- PostgreSQL (lub dostęp do bazy danych PostgreSQL)

## 🚀 Instalacja

### 1. Klonowanie repozytorium
```bash
git clone <url-repozytorium>
cd ForumDyskusyjne
```

### 2. Instalacja pakietów NuGet
```bash
dotnet restore
```

### 3. Konfiguracja bazy danych

1. Skopiuj plik przykładowej konfiguracji:
   ```bash
   cp appsettings.example.json appsettings.json
   ```

2. Edytuj plik `appsettings.json` i wprowadź swoje dane do bazy danych:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Host=twój_host; Database=twoja_baza; Username=twój_użytkownik; Password=twoje_hasło; SSL Mode=Prefer;"
     }
   }
   ```

### 4. Uruchomienie aplikacji
```bash
dotnet run
```

## 📦 Wykorzystane pakiety

- **Microsoft.Extensions.Configuration** (8.0.0) - Zarządzanie konfiguracją
- **Microsoft.Extensions.Configuration.Json** (8.0.0) - Obsługa plików JSON w konfiguracji
- **Npgsql** (8.0.3) - Driver PostgreSQL dla .NET

## 🔧 Konfiguracja

### Connection String
Aplikacja wykorzystuje PostgreSQL jako bazę danych. Connection string należy umieścić w pliku `appsettings.json`.

### Zmienne środowiskowe (opcjonalnie)
Alternatywnie można używać zmiennych środowiskowych:
```bash
export ConnectionStrings__DefaultConnection="Host=localhost;Database=forum;Username=user;Password=pass"
```

## 🔒 Bezpieczeństwo

- Plik `appsettings.json` jest dodany do `.gitignore` i nie będzie commitowany
- Używaj silnych haseł do bazy danych
- W produkcji używaj zmiennych środowiskowych lub Azure Key Vault

## 📝 Notatki deweloperskie

- Aplikacja testuje połączenie z bazą danych przy starcie
- Connection string jest ładowany z pliku `appsettings.json`
- Obsługiwane są różne środowiska (Development, Production)
