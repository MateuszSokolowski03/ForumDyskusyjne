# TODO - Forum Dyskusyjne

## 🎯 Kolejne kroki implementacji (w kolejności priorytetów)

1. **Utworzenie pierwszego Controller** - AuthController dla testów
2. **Przeniesienie API auth** - Przenieść logikę logowania z Program.cs do AuthController
3. **Testy funkcjonalności** - Sprawdzić czy logowanie nadal działa
4. **Dodanie kolejnych Controllers** - ForumController, PostController, etc.
5. **Implementacja logiki biznesowej** - Services i Repository patterns

---

## 🚀 PRIORYTET WYSOKI (Podstawowa funkcjonalność)

### 1. Refaktoryzacja Backend - Architektura MVC
- [ ] **Przeniesienie logiki z Program.cs do Controllers**
  - [ ] Utworzenie AuthController (login, register, logout, status)
  - [ ] Utworzenie ForumController (kategorie, wątki)
  - [ ] Utworzenie PostController (wiadomości w wątkach)
  - [ ] Utworzenie UserController (profil, edycja danych)
  - [ ] Utworzenie AdminController (zarządzanie forum)

- [x] **Warstwa danych (Data Layer) - UKOŃCZONA ✅**
  - [x] Implementacja DbContext (Entity Framework Core) - ✅ ForumDbContext.cs
  - [x] Konfiguracja Entity Framework w Program.cs - ✅ 
  - [x] Wszystkie modele C# (17 modeli) - ✅ User, Category, Forum, Thread, Message, etc.
  - [x] Migracje bazy danych - ✅ InitialCreate zastosowana
  - [x] Wszystkie DbSets skonfigurowane - ✅ 17 tabel utworzonych
  - [ ] Repository Pattern (opcjonalnie)

### 2. API Endpoints - Podstawowe funkcjonalności forum
- [ ] **Forum/Wątki/Wiadomości**
  - [ ] `GET /api/categories` - lista kategorii
  - [ ] `GET /api/categories/{id}/threads` - wątki w kategorii
  - [ ] `POST /api/threads` - tworzenie nowego wątku
  - [ ] `GET /api/threads/{id}` - szczegóły wątku
  - [ ] `POST /api/threads/{id}/posts` - dodanie wiadomości do wątku
  - [ ] `PUT /api/posts/{id}` - edycja wiadomości
  - [ ] `DELETE /api/posts/{id}` - usunięcie wiadomości

- [ ] **Profil użytkownika**
  - [ ] `GET /api/user/profile` - pobranie profilu
  - [ ] `PUT /api/user/profile` - edycja profilu
  - [ ] `PUT /api/user/password` - zmiana hasła

### 3. Frontend - Integracja z API
- [ ] **Aktualizacja JavaScript w istniejących stronach**
  - [ ] Integracja forum.html z API kategorii i wątków
  - [ ] Integracja thread.html z API wiadomości w wątkach
  - [ ] Aktualizacja formularzy do wysyłania danych przez API
  - [ ] Obsługa błędów i komunikatów zwrotnych
  - [ ] Loading states i spinners

### 4. Podstawowy system uprawnień
- [ ] **Role użytkowników**
  - [ ] Enum UserRole: User, Moderator, Admin
  - [ ] Authorization w Controllers (sprawdzanie ról)
  - [ ] Middleware autoryzacji
- [ ] **Uprawnienia użytkowników anonimowych**
  - [ ] Rozszerzenie tabeli categories o `allow_anonymous_view`, `allow_anonymous_post`
  - [ ] Logika sprawdzania uprawnień w API
  - [ ] Frontend - ukrywanie/pokazywanie treści

---

## 🔧 PRIORYTET ŚREDNI (Funkcjonalności rozszerzające)

### 5. Panel administracyjny - Zarządzanie użytkownikami
- [ ] **Backend - API administracyjne**
  - [ ] `GET /api/admin/users` - lista wszystkich użytkowników z filtrowaniem
  - [ ] `GET /api/admin/users/{id}` - szczegóły użytkownika dla admina
  - [ ] `PUT /api/admin/users/{id}/ban` - zbanowanie użytkownika
  - [ ] `PUT /api/admin/users/{id}/unban` - odbanowanie użytkownika
  - [ ] `PUT /api/admin/users/{id}/role` - zmiana roli użytkownika
  - [ ] `DELETE /api/admin/users/{id}` - usunięcie użytkownika
  - [ ] `POST /api/admin/users/{id}/warning` - dodanie ostrzeżenia

- [ ] **Frontend - Panel admina**
  - [ ] Rozbudowa admin/users.html
  - [ ] Lista użytkowników z filtrowaniem i wyszukiwaniem
  - [ ] Formularz banowania użytkowników
  - [ ] Zmiana ról użytkowników
  - [ ] Historia działań administratora

### 6. Panel administracyjny - Zarządzanie treścią
- [ ] **Backend - API zarządzania postami**
  - [ ] `GET /api/admin/posts` - lista wszystkich postów z filtrowaniem
  - [ ] `GET /api/admin/posts/reported` - lista zgłoszonych postów
  - [ ] `PUT /api/admin/posts/{id}/approve` - zatwierdzenie postu
  - [ ] `PUT /api/admin/posts/{id}/reject` - odrzucenie postu
  - [ ] `DELETE /api/admin/posts/{id}` - usunięcie postu przez admina

- [ ] **Backend - API zarządzania kategoriami**
  - [ ] `POST /api/admin/categories` - tworzenie nowej kategorii
  - [ ] `PUT /api/admin/categories/{id}` - edycja kategorii (nazwa, opis, ikona)
  - [ ] `DELETE /api/admin/categories/{id}` - usuwanie kategorii
  - [ ] `PUT /api/admin/categories/reorder` - zmiana kolejności kategorii
  - [ ] `PUT /api/admin/categories/{id}/toggle` - aktywacja/deaktywacja kategorii

- [ ] **Frontend - Panel zarządzania treścią**
  - [ ] admin/categories.html - zarządzanie kategoriami
  - [ ] admin/threads.html - moderacja wątków
  - [ ] Narzędzia do masowych operacji

### 7. System moderatorów

- [ ] **API endpointy dla moderatorów**
  - [ ] `POST /api/admin/categories/{id}/moderators` - przydzielenie moderatora
  - [ ] `DELETE /api/admin/categories/{id}/moderators/{userId}` - usunięcie moderatora
  - [ ] `GET /api/admin/categories/{id}/moderators` - lista moderatorów kategorii
  - [ ] `PUT /api/moderator/posts/{id}` - edycja postu przez moderatora
  - [ ] `DELETE /api/moderator/posts/{id}` - usunięcie postu przez moderatora

- [ ] **Frontend - Panel moderatorów**
  - [ ] admin/moderators.html - zarządzanie moderatorami
  - [ ] Interfejs moderatora dla uprawnień specjalnych

### 8. Wątki przyklejone i ogłoszenia
- [ ] **Wątki przyklejone**
  - [ ] `PUT /api/admin/threads/{id}/pin` - przyklejenie wątku (tylko admin)
  - [ ] `PUT /api/admin/threads/{id}/unpin` - odepinowanie wątku (tylko admin)
  - [ ] Frontend - sortowanie listy wątków (przyklejone na górze)
  - [ ] Wizualne oznaczenie przyklejonych wątków

- [ ] **Ogłoszenia administracyjne**
  - [ ] API: `GET /api/announcements/active`, `POST /api/admin/announcements`
  - [ ] Frontend - sekcja ogłoszeń na stronie głównej

---

## 🎨 PRIORYTET NIŻSZY (Funkcjonalności dodatkowe)

### 9. Awatary użytkowników
- [ ] **Backend - upload i zarządzanie plikami**
  - [ ] `POST /api/user/avatar` - upload avatara
  - [ ] Walidacja plików (rozmiar, format)
  - [ ] Generowanie miniaturek
  - [ ] Przechowywanie plików (wwwroot/uploads/avatars/)

- [ ] **Frontend - awatary**
  - [ ] Wyświetlanie awatarów przy postach i w profilu
  - [ ] Formularz uploadu avatara w profilu użytkownika
  - [ ] Awatar domyślny dla użytkowników bez własnego

### 10. Wiadomości prywatne

- [ ] **API endpointy**
  - [ ] `GET /api/messages` - lista wiadomości użytkownika
  - [ ] `POST /api/messages` - wysłanie nowej wiadomości
  - [ ] `PUT /api/messages/{id}/read` - oznaczenie jako przeczytane
  - [ ] `DELETE /api/messages/{id}` - usunięcie wiadomości
  - [ ] `GET /api/messages/unread-count` - liczba nieprzeczytanych

- [ ] **Frontend - system wiadomości**
  - [ ] Rozszerzenie messages.html
  - [ ] Formularz wysyłania wiadomości
  - [ ] Powiadomienia o nowych wiadomościach

### 11. Stronicowanie i wyszukiwanie
- [ ] **Stronicowanie list**
  - [ ] Backend - parametry page, pageSize w API
  - [ ] Frontend - komponenty paginacji dla wszystkich list
  - [ ] Lazy loading dla długich list

- [ ] **Wyszukiwanie w treści**
  - [ ] `GET /api/search?q={query}&category={categoryId}` - wyszukiwanie
  - [ ] Full-text search w PostgreSQL
  - [ ] Operatory AND/OR/NOT w wyszukiwaniu
  - [ ] Frontend - strona wyników wyszukiwania
  - [ ] Podświetlanie znalezionych fraz

### 12. Załączniki do wiadomości

- [ ] **Backend - obsługa załączników**
  - [ ] `POST /api/posts/{id}/attachments` - upload załącznika
  - [ ] `DELETE /api/attachments/{id}` - usunięcie załącznika
  - [ ] Walidacja plików (rozmiar, typy MIME)

- [ ] **Frontend - załączniki**
  - [ ] Drag&drop upload w edytorze postów
  - [ ] Lista załączników przy wyświetlaniu postu
  - [ ] Podgląd obrazków, download innych plików

---

## 🛡️ PRIORYTET ZAAWANSOWANY (Bezpieczeństwo i moderacja)

### 13. System zgłoszeń do moderacji

- [ ] **Backend - API zgłoszeń**
  - [ ] `POST /api/posts/{id}/report` - zgłoszenie postu
  - [ ] `GET /api/admin/reports` - lista zgłoszeń dla moderatorów
  - [ ] `PUT /api/admin/reports/{id}/resolve` - rozwiązanie zgłoszenia

- [ ] **Frontend - zgłoszenia**
  - [ ] Przycisk "Zgłoś" przy każdym poście
  - [ ] Modal z formularzem zgłoszenia
  - [ ] Panel moderatora - lista zgłoszeń

### 14. Słownik słów zakazanych z automatyczną moderacją

- [ ] **Backend - API zarządzania słownikiem**
  - [ ] `GET /api/admin/banned-words` - lista słów z paginacją
  - [ ] `POST /api/admin/banned-words` - dodanie słowa
  - [ ] `PUT /api/admin/banned-words/{id}` - edycja reguły
  - [ ] `DELETE /api/admin/banned-words/{id}` - usunięcie słowa
  - [ ] `POST /api/admin/banned-words/import` - import listy (CSV/JSON)
  - [ ] `GET /api/admin/banned-words/statistics` - statystyki wykryć

- [ ] **Middleware automatycznej moderacji**
  - [ ] Sprawdzanie treści przed zapisem do bazy
  - [ ] Algorytm dopasowywania (exact, contains, regex)
  - [ ] Działania według severity_level:
    - Warning: zapis + oznaczenie do moderacji
    - Block: odrzucenie + komunikat
    - AutoDelete: zapis jako ukryty + powiadomienie
  - [ ] Cache słownika w pamięci
  - [ ] Ignorowanie wielkości liter, diakrytyków

- [ ] **Frontend - panel słownika**
  - [ ] admin/banned-words.html - zarządzanie słowami
  - [ ] Import/export słownika (drag&drop)
  - [ ] Test regexów na żywo
  - [ ] Statystyki wykryć

- [ ] **Ograniczenia**
  - [ ] Max 1000 słów w słowniku
  - [ ] Walidacja regex przed zapisem
  - [ ] Backup przy każdej zmianie
  - [ ] Audit log zmian

### 15. Automatyczne wylogowanie po bezczynności
- [ ] **Backend - śledzenie sesji**
  - [ ] Konfiguracja timeout sesji
  - [ ] API endpoint do sprawdzania ważności sesji
  - [ ] `GET /api/auth/heartbeat` - przedłużanie sesji

- [ ] **Frontend - JavaScript timer**
  - [ ] Licznik bezczynności
  - [ ] Ostrzeżenie przed wylogowaniem
  - [ ] Automatyczne wylogowanie i przekierowanie

---

## 🎯 FUNKCJONALNOŚCI ZAAWANSOWANE (Przyszłe rozszerzenia)

### 16. System rang użytkowników

- [ ] **Automatyczne nadawanie rang**
  - [ ] Na podstawie liczby postów
  - [ ] Na podstawie czasu rejestracji
  - [ ] Specjalne rangi nadawane ręcznie

### 17. Emotikony w wiadomościach
- [ ] **System emotikon**
  - [ ] Parsowanie emotikon w treści (:smile:, :wink:)
  - [ ] Zestaw standardowych emotikon
  - [ ] Upload niestandardowych emotikon przez admina

### 18. Wiadomości w formacie HTML
- [ ] **Bezpieczny HTML**
  - [ ] Whitelist dozwolonych znaczników
  - [ ] Sanityzacja HTML przed zapisem
  - [ ] WYSIWYG editor dla użytkowników

### 19. Bezpieczna zmiana hasła

- [ ] **Bezpieczeństwo haseł**
  - [ ] Wymagania co do złożoności hasła
  - [ ] Historia haseł (nie można używać ostatnich N haseł)
  - [ ] Rate limiting dla prób reset hasła

---

## 📊 METRYKI I MONITORING

### 20. Statystyki forum
- [ ] **Dashboard administratora**
  - [ ] Liczba użytkowników, postów, wątków
  - [ ] Statystyki aktywności (dzienne, tygodniowe)
  - [ ] Najpopularniejsze kategorie
  - [ ] Wykres aktywności w czasie

### 21. Logi i audyt
- [ ] **System logowania**
  - [ ] Logi akcji użytkowników
  - [ ] Logi akcji administratorów
  - [ ] Logi błędów systemowych

---

## ✅ UKOŃCZONE
- [x] **Struktura projektu** - ASP.NET Core Web API + statyczne pliki HTML/CSS/JS
- [x] **Pełna struktura bazy danych** - 17 tabel utworzonych w PostgreSQL
- [x] **Wszystkie modele C#** - User, Category, Forum, Thread, Message, Attachment, UserRank, ForumModerator, ForumPermission, BannedWord, Announcement, PrivateMessage, Report, PasswordResetToken, UserRankHistory, ContentModerationLog, AdminAction, Enums
- [x] **Entity Framework Core** - DbContext z konfiguracją wszystkich relacji
- [x] **Migracje** - InitialCreate zastosowana pomyślnie
- [x] **Podstawowe strony HTML** - login, register, forum, thread, admin panel
- [x] **Style CSS** - responsywny design z Tailwind CSS
- [x] **Podstawowy JavaScript** - logika logowania, rejestracji, nawigacji
- [x] **Konfiguracja autoryzacji** - JWT tokens w Program.cs
- [x] **Podstawowe API endpointy** - login, register w Program.cs

---

## 📝 NOTATKI TECHNICZNE

### Baza danych
- **PostgreSQL** - główna baza danych
- **Entity Framework Core** - ORM
- **Migracje** - zarządzanie schematem bazy

### Architektura
- **ASP.NET Core Web API** - backend
- **MVC Pattern** - organizacja kodu
- **JWT Authentication** - autoryzacja
- **Static Files** - frontend HTML/CSS/JS

### Frontend
- **Vanilla JavaScript** - bez frameworków
- **Tailwind CSS** - stylowanie
- **Responsive Design** - dostosowanie do urządzeń mobilnych

### Deployment
- **Linux** - środowisko produkcyjne
- **Nginx** - reverse proxy (opcjonalnie)
- **Systemd** - zarządzanie usługą
