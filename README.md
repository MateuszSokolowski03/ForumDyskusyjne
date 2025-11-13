# Forum Dyskusyjne

## 📋 Wymagania
- .NET 8.0 SDK
- PostgreSQL

## 🚀 Szybki start

1. **Sklonuj repozytorium**
   ```bash
   git clone <url-repozytorium>
   cd ForumDyskusyjne
   ```

2. **Uruchom setup**
   ```bash
   ./setup.sh
   ```

3. **Skonfiguruj bazę danych**
   - Edytuj `appsettings.json`
   - Wprowadź swoje dane PostgreSQL

4. **Uruchom aplikację**
   ```bash
   dotnet run
   ```

5. **Otwórz w przeglądarce**
   - http://localhost:5000

## 📦 Technologie
- .NET 8.0 Web API
- PostgreSQL
- Tailwind CSS
- Material Symbols

## 🎨 Funkcje
- 🏠 Responsywny interfejs
- 💻 Kategorie forum (Technologie, Rozrywka, Społeczność)
- 📊 Statystyki w czasie rzeczywistym
- 🔍 Wyszukiwarka
- 🌙 Dark mode

## � API Endpoints
- `GET /` - Strona główna
- `GET /api/status` - Status aplikacji i bazy danych
