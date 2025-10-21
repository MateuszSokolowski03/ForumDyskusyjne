#!/bin/bash

# 🚀 SZYBKI START - Forum Dyskusyjne

echo "🔧 Przygotowywanie środowiska Forum Dyskusyjne..."

# Sprawdź czy .NET 8 jest zainstalowany
if ! command -v dotnet &> /dev/null; then
    echo "❌ .NET SDK nie jest zainstalowane!"
    echo "📥 Pobierz ze strony: https://dotnet.microsoft.com/download"
    exit 1
fi

echo "✅ .NET SDK znalezione: $(dotnet --version)"

# Przywróć pakiety NuGet
echo "📦 Instalowanie pakietów NuGet..."
dotnet restore

if [ $? -eq 0 ]; then
    echo "✅ Pakiety NuGet zainstalowane pomyślnie"
else
    echo "❌ Błąd podczas instalacji pakietów NuGet"
    exit 1
fi

# Sprawdź czy plik konfiguracyjny istnieje
if [ ! -f "appsettings.json" ]; then
    echo "⚠️  Brak pliku appsettings.json"
    echo "📋 Kopiowanie pliku przykładowego..."
    cp appsettings.example.json appsettings.json
    echo "✅ Plik appsettings.json utworzony"
    echo ""
    echo "🔧 WAŻNE: Edytuj plik appsettings.json i wprowadź swoje dane do bazy danych!"
    echo "📝 nano appsettings.json"
    echo ""
fi

# Sprawdź czy konfiguracja jest poprawna
if grep -q "your_database\|your_username\|your_password\|localhost" appsettings.json; then
    echo "⚠️  UWAGA: Plik appsettings.json zawiera przykładowe dane!"
    echo "🔧 Przed uruchomieniem aplikacji zaktualizuj connection string."
    echo ""
fi

echo "🎉 Środowisko przygotowane!"
echo ""
echo "🚀 Aby uruchomić aplikację wykonaj:"
echo "   dotnet run"
echo ""
echo "📚 Więcej informacji znajdziesz w pliku README.md"
