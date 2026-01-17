/**
 * main.js - Główny plik logiki forum
 * Zawiera: Uwierzytelnianie, Globalne UI, Inteligentne Wyszukiwanie
 */

// Globalna flaga zapobiegająca duplikowaniu event listenerów
let _avatarDelegationSetup = false;

if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', initializeApp);
} else {
    initializeApp();
}

function initializeApp() {
    setupSearchFunctionality();
    setupSearchDropdown();
    setupThemeToggle();
    setupForumInteractions();
    setupAuthenticationUI(); 
    setupAvatarDelegation();
    
    console.log('🚀 Forum Dyskusyjne uruchomione pomyślnie');
}

// ==========================================
// 🔐 SEKCJA UWIERZYTELNIANIA
// ==========================================

async function setupAuthenticationUI() {
    console.log('🔐 Konfigurowanie uwierzytelniania UI...');

    try {
        const currentUser = await getCurrentUser();
        updateTopBarUI(currentUser);
    } catch (error) {
        console.error('❌ Błąd konfigurowania uwierzytelniania UI:', error);
        updateTopBarUI(null);
    }
}

async function getCurrentUser() {
    try {
        // 1. Sprawdź sesję
        const statusRes = await fetch('/api/auth/status', {
            headers: { 'Accept': 'application/json' },
            credentials: 'include'
        });

        if (statusRes.status === 403) {
            await logout(); 
            return null;
        }
        
        if (!statusRes.ok) return null; 

        let userData = await statusRes.json();
        
        // Normalizacja danych
        if (userData.user) {
            userData = { ...userData.user, role: userData.role || userData.user.role };
        }

        // 2. Pobierz pełny profil dla avatara
        if (userData && userData.id) {
            try {
                const profileRes = await fetch(`/api/users/${userData.id}`, { 
                    headers: { 'Accept': 'application/json' },
                    credentials: 'include' 
                });
                
                if (profileRes.ok) {
                    const fullProfile = await profileRes.json();
                    if (fullProfile.avatarUrl || fullProfile.AvatarUrl) {
                        userData.avatarUrl = fullProfile.avatarUrl || fullProfile.AvatarUrl;
                    }
                }
            } catch (e) {
                console.warn('⚠️ Nie udało się dociągnąć szczegółów profilu:', e);
            }
        }

        return userData;

    } catch (error) {
        console.error('❌ Błąd sprawdzania statusu:', error);
        return null;
    }
}

function updateTopBarUI(currentUser) {
    const authContainer = document.getElementById('auth-container');
    if (!authContainer) return;

    // 1. Wariant dla niezalogowanego
    if (!currentUser) {
        authContainer.innerHTML = `
            <div class="flex items-center gap-2">
                <a href="/login.html" class="text-sm font-medium text-gray-700 dark:text-gray-200 hover:text-primary px-3 py-2">Logowanie</a>
                <a href="/register.html" class="text-sm font-medium bg-primary text-white px-4 py-2 rounded-lg hover:bg-primary/90 shadow-sm transition-colors">Rejestracja</a>
            </div>
        `;
        return;
    }

    // 2. Wariant dla zalogowanego
    const avatarUrl = currentUser.avatarUrl || currentUser.AvatarUrl || currentUser.avatar;
    const username = currentUser.username || 'Użytkownik';
    const initial = username.charAt(0).toUpperCase();
    
    const avatarStyle = avatarUrl 
        ? `background-image:url('${avatarUrl}');background-size:cover;background-position:center;` 
        : '';

    authContainer.innerHTML = `
        <div class="relative flex items-center"> 
            <button id="global-avatar-btn" 
                    class="w-10 h-10 bg-blue-500 text-white rounded-full hover:ring-2 hover:ring-blue-400/50 font-semibold flex items-center justify-center cursor-pointer overflow-hidden shadow-sm border border-gray-200 dark:border-gray-600 transition-all"
                    style="${avatarStyle}"
                    title="${escapeHtml(username)}"> ${!avatarUrl ? initial : ''}
            </button>
            
            <div id="global-avatar-menu" class="hidden absolute right-0 top-full mt-2 w-56 bg-white dark:bg-[#192231] rounded-xl shadow-xl border border-gray-200 dark:border-gray-700 py-2 z-50 origin-top-right transform transition-all duration-200">
                <div class="px-4 py-3 border-b border-gray-200 dark:border-gray-700 bg-gray-50 dark:bg-[#1e293b]/50">
                    <p class="text-xs text-gray-500 dark:text-gray-400">Zalogowano jako</p>
                    <p class="text-sm font-bold text-gray-900 dark:text-white truncate">${escapeHtml(username)}</p>
                </div>
                
                <a href="/profile.html" class="block px-4 py-2 text-sm text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-[#2a3851] transition-colors flex items-center">
                    <span class="material-symbols-outlined text-[20px] mr-2">person</span>
                    Mój profil
                </a>
                <a href="/messages.html" class="block px-4 py-2 text-sm text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-[#2a3851] transition-colors flex items-center">
                    <span class="material-symbols-outlined text-[20px] mr-2">mail</span>
                    Wiadomości
                </a>
                
                <div class="border-t border-gray-100 dark:border-gray-700 my-1"></div>
                
                <button id="global-logout-btn" class="w-full text-left px-4 py-2 text-sm text-red-600 dark:text-red-400 hover:bg-red-50 dark:hover:bg-red-900/20 transition-colors flex items-center">
                    <span class="material-symbols-outlined text-[20px] mr-2">logout</span>
                    Wyloguj się
                </button>
            </div>
        </div>
    `;

    // Event listenery dla menu avatara
    const btn = document.getElementById('global-avatar-btn');
    const menu = document.getElementById('global-avatar-menu');
    const logoutBtn = document.getElementById('global-logout-btn');

    if (btn && menu) {
        btn.addEventListener('click', (e) => {
            e.stopPropagation();
            menu.classList.toggle('hidden');
        });

        document.addEventListener('click', (e) => {
            if (!btn.contains(e.target) && !menu.contains(e.target)) {
                menu.classList.add('hidden');
            }
        });

        document.addEventListener('keydown', (e) => {
            if (e.key === 'Escape') menu.classList.add('hidden');
        });
    }

    if (logoutBtn) {
        logoutBtn.addEventListener('click', async (e) => {
            e.preventDefault();
            await logout();
        });
    }
}

async function logout() {
    try {
        await fetch('/api/auth/logout', { method: 'POST', credentials: 'include' });
    } catch (e) {
        console.error('Logout error', e);
    }
    document.cookie = 'user_session=; expires=Thu, 01 Jan 1970 00:00:00 UTC; path=/;';
    window.location.href = '/login.html';
}

// ==========================================
// 🔍 ZAAWANSOWANE WYSZUKIWANIE (NOWE)
// ==========================================

function setupSearchFunctionality() {
    const searchInputs = document.querySelectorAll('input[placeholder="Szukaj..."]');
    
    searchInputs.forEach(input => {
        // Debounce: czekamy 300ms po zakończeniu pisania przed wysłaniem zapytania
        input.addEventListener('input', debounce(function(e) {
            const query = e.target.value.trim();
            if (query.length > 1) { // Szukaj dopiero, gdy są min. 2 znaki
                performSearch(query, input);
            } else {
                hideSearchResults();
            }
        }, 300));

        // Obsługa klawiatury
        input.addEventListener('keydown', (e) => {
            if (e.key === 'Escape') hideSearchResults();
        });

        // Pokaż wyniki ponownie po kliknięciu w input (jeśli jest tekst)
        input.addEventListener('focus', (e) => {
            if (e.target.value.trim().length > 1) {
                const resultsPanel = getResultsPanel(e.target);
                if (resultsPanel && resultsPanel.innerHTML !== '') {
                    resultsPanel.classList.remove('hidden');
                }
            }
        });
    });

    // Zamknij wyniki klikając poza
    document.addEventListener('click', (e) => {
        if (!e.target.closest('#search-container') && !e.target.closest('.search-results-panel')) {
            hideSearchResults();
        }
    });
}

function setupSearchDropdown() {
    const container = document.getElementById('search-container');
    if (!container) return;

    const input = container.querySelector('#search-input');
    const closeBtn = container.querySelector('#search-close');
    const toggle = document.getElementById('search-toggle');

    // Obsługa przycisku lupy (np. na mobile)
    if (toggle) {
        toggle.addEventListener('click', (e) => {
            e.preventDefault();
            container.classList.remove('hidden');
            container.classList.add('flex');
            if (input) input.focus();
        });
    }

    // Obsługa krzyżyka (zamknij i wyczyść)
    if (closeBtn) {
        closeBtn.addEventListener('click', () => {
            container.classList.add('hidden');
            container.classList.remove('flex');
            hideSearchResults();
            if (input) input.value = '';
        });
    }
}

// Tworzy lub pobiera panel wyników dla konkretnego inputa
function getResultsPanel(inputElement) {
    // Input znajduje się zazwyczaj w div.relative
    const parent = inputElement.parentElement; 
    let panel = parent.querySelector('.search-results-panel');
    
    if (!panel) {
        panel = document.createElement('div');
        
        // POPRAWIONE STYLE:
        // 1. 'right-0' zamiast 'left-0' -> żeby nie wychodziło poza ekran z prawej strony
        // 2. 'min-w-[300px]' -> żeby panel nie był zbyt wąski, nawet jak input jest mały
        panel.className = 'search-results-panel absolute right-0 top-full mt-2 w-full min-w-[300px] max-w-[90vw] max-h-[80vh] overflow-y-auto bg-white dark:bg-[#192231] rounded-xl border border-gray-200 dark:border-gray-700 shadow-2xl z-50 hidden custom-scrollbar';
        
        parent.appendChild(panel);
    }
    return panel;
}

function hideSearchResults() {
    document.querySelectorAll('.search-results-panel').forEach(el => el.classList.add('hidden'));
}

async function performSearch(query, inputElement) {
    const panel = getResultsPanel(inputElement);
    panel.classList.remove('hidden');
    
    // Stan ładowania
    panel.innerHTML = `
        <div class="p-4 text-center text-gray-500 dark:text-gray-400 text-sm">
            <span class="inline-block animate-spin mr-2">⏳</span> Szukanie: "<b>${escapeHtml(query)}</b>"...
        </div>
    `;

    try {
        // Zapytanie do backendu. Oczekiwany format JSON: { forums: [], threads: [] }
        const response = await fetch(`/api/search?q=${encodeURIComponent(query)}`, {
            headers: { 'Accept': 'application/json' }
        });

        if (!response.ok) throw new Error('Network error');
        const data = await response.json();
        
        // Zabezpieczenie przed brakiem tablic
        const forums = data.forums || [];
        const threads = data.threads || [];

        if (forums.length === 0 && threads.length === 0) {
            panel.innerHTML = `
                <div class="p-4 text-center text-gray-500 dark:text-gray-400 text-sm">
                    Brak wyników dla "<b>${escapeHtml(query)}</b>"
                </div>
            `;
            return;
        }

        let html = '';

        // --- SEKCJA FORÓW (KATEGORII) ---
        if (forums.length > 0) {
            html += `
                <div class="px-4 py-2 bg-gray-50 dark:bg-[#232f48] border-b border-gray-100 dark:border-gray-700">
                    <h3 class="text-xs font-bold text-gray-500 dark:text-gray-400 uppercase tracking-wider">Kategorie</h3>
                </div>
            `;
            forums.forEach(forum => {
                html += `
                    <a href="/forum.html?forumId=${forum.id}" class="block px-4 py-3 hover:bg-gray-50 dark:hover:bg-[#2a3851] border-b border-gray-100 dark:border-gray-700 last:border-0 transition-colors group">
                        <div class="flex items-center gap-3">
                            <span class="material-symbols-outlined text-primary group-hover:scale-110 transition-transform">folder</span>
                            <div>
                                <div class="font-medium text-gray-900 dark:text-white text-sm">${highlightMatch(forum.name, query)}</div>
                                <div class="text-xs text-gray-500 dark:text-gray-400 truncate max-w-[250px]">${escapeHtml(forum.description || '')}</div>
                            </div>
                        </div>
                    </a>
                `;
            });
        }

        // --- SEKCJA WĄTKÓW ---
        if (threads.length > 0) {
            // Jeśli były fora, dodaj separator
            html += `
                <div class="px-4 py-2 bg-gray-50 dark:bg-[#232f48] border-b border-gray-100 dark:border-gray-700 ${forums.length > 0 ? 'border-t' : ''}">
                    <h3 class="text-xs font-bold text-gray-500 dark:text-gray-400 uppercase tracking-wider">Tematy</h3>
                </div>
            `;
            threads.forEach(thread => {
                html += `
                    <a href="/thread.html?threadId=${thread.id}" class="block px-4 py-3 hover:bg-gray-50 dark:hover:bg-[#2a3851] border-b border-gray-100 dark:border-gray-700 last:border-0 transition-colors group">
                        <div class="flex items-center gap-3">
                            <span class="material-symbols-outlined text-gray-400 group-hover:text-primary transition-colors">chat</span>
                            <div class="flex-1 min-w-0">
                                <div class="font-medium text-gray-900 dark:text-white text-sm truncate">${highlightMatch(thread.title, query)}</div>
                                <div class="flex items-center gap-2 text-xs text-gray-500 dark:text-gray-400 mt-0.5">
                                    <span>${escapeHtml(thread.authorName || 'Anonim')}</span>
                                    <span>•</span>
                                    <span>${thread.repliesCount || 0} odp.</span>
                                </div>
                            </div>
                        </div>
                    </a>
                `;
            });
        }

        panel.innerHTML = html;

    } catch (error) {
        console.error('Błąd wyszukiwania:', error);
        // Jeśli API nie działa, pokaż cichy błąd w UI
        panel.innerHTML = `
            <div class="p-4 text-center text-red-400 text-sm">
                Nie udało się pobrać wyników.
            </div>
        `;
    }
}

// Funkcja pomocnicza: podświetla znalezioną frazę w tekście
function highlightMatch(text, query) {
    if (!text) return '';
    const safeText = escapeHtml(text);
    const safeQuery = escapeHtml(query).replace(/[.*+?^${}()|[\]\\]/g, '\\$&'); // Escape regex chars
    const regex = new RegExp(`(${safeQuery})`, 'gi');
    return safeText.replace(regex, '<span class="text-primary font-bold bg-blue-100 dark:bg-blue-900/30 rounded px-1">$1</span>');
}

// ==========================================
// ⚙️ UI I POMOCNICZE
// ==========================================

function setupThemeToggle() {
    const savedTheme = localStorage.getItem('theme') || 'dark';
    applyTheme(savedTheme);
}

function applyTheme(theme) {
    const html = document.documentElement;
    if (theme === 'dark') html.classList.add('dark');
    else html.classList.remove('dark');
    localStorage.setItem('theme', theme);
}

function setupForumInteractions() {
    document.querySelectorAll('.forum-thread').forEach(thread => {
        thread.addEventListener('click', function(e) {
            if (e.target.closest('a')) return;
            const link = thread.querySelector('a');
            if (link) window.location.href = link.href;
        });
    });
}

function setupAvatarDelegation() {
    if (_avatarDelegationSetup) return;
    _avatarDelegationSetup = true;
    
    document.addEventListener('click', function(e) {
        const btn = e.target.closest('.js-avatar-btn');
        if (btn) {
            e.stopPropagation();
            const wrapper = btn.closest('div');
            const menu = wrapper?.querySelector('.js-avatar-menu');
            if (menu) menu.classList.toggle('hidden');
        } else {
            document.querySelectorAll('.js-avatar-menu:not(.hidden)').forEach(m => m.classList.add('hidden'));
        }
    });
}

// Utility: Debounce (opóźnienie wykonania funkcji)
function debounce(func, wait) {
    let timeout;
    return function(...args) {
        clearTimeout(timeout);
        timeout = setTimeout(() => func(...args), wait);
    };
}

// Utility: Bezpieczny HTML
window.escapeHtml = function(text) {
    if (!text && text !== 0) return '';
    return String(text).replace(/[&<>"']/g, m => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#039;' })[m]);
};

// Utility: Wrapper na fetch
window.fetchJson = async function(url, opts = {}) {
    try {
        const res = await fetch(url, Object.assign({ credentials: 'include', headers: { 'Accept': 'application/json' } }, opts));
        if (!res.ok) return { ok: false, status: res.status, json: null };
        return { ok: true, status: res.status, json: await res.json() };
    } catch (e) {
        return { ok: false, status: 0, json: null };
    }
};

// Eksport API dla innych skryptów
window.ForumApp = {
    logout,
    updateTopBarUI,
    getCurrentUser
};