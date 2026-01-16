// Global flag to ensure setupAvatarDelegation runs only once
let _avatarDelegationSetup = false;

if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', initializeApp);
} else {
    // If script is loaded after DOMContentLoaded, initializeApp();
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

// Funkcjonalność wyszukiwania
function setupSearchFunctionality() {
    const searchInputs = document.querySelectorAll('input[placeholder="Szukaj..."]');
    
    searchInputs.forEach(input => {
        input.addEventListener('input', debounce(function(e) {
            const query = e.target.value.trim();
            if (query.length > 0) {
                performSearch(query);
            }
        }, 300));
    });
}

// Dropdown z kategoriami i forami pod przyciskiem Szukaj
function setupSearchDropdown() {
    const container = document.getElementById('search-container');
    if (!container) return;

    const input = container.querySelector('#search-input');
    const closeBtn = container.querySelector('#search-close');
    if (!input) return;

    // Create panel attached to body for consistent positioning
    let panel = document.querySelector('.search-results-panel');
    if (!panel) {
        panel = document.createElement('div');
        panel.className = 'search-results-panel bg-white dark:bg-[#192231] rounded-lg border border-gray-200 dark:border-gray-700 shadow-lg z-50 p-1';
        panel.style.position = 'fixed';
        panel.style.display = 'none';
        panel.style.maxHeight = '320px';
        panel.style.overflow = 'auto';
        panel.style.minWidth = '220px';
        document.body.appendChild(panel);
    }

    let items = [];

    async function loadItems() {
        if (items.length) return;
        try {
            const res = await fetchJson('/api/forum/categories');
            if (!res.ok || !Array.isArray(res.json)) return;
            const categories = res.json;
            const list = [];
            categories.forEach(cat => {
                list.push({ type: 'category', id: cat.id, name: cat.name });
                (cat.forums || []).forEach(f => list.push({ type: 'forum', id: f.id, name: f.name, categoryId: cat.id, categoryName: cat.name }));
            });
            items = list;
        } catch (e) {
            console.warn('search dropdown load error', e);
        }
    }

    function positionPanel() {
        const rect = input.getBoundingClientRect();
        const spacing = 8;
        const desiredWidth = Math.max(220, Math.min(420, rect.width));
        let left = rect.left;
        let top = rect.bottom + spacing;

        // If panel would overflow right edge, shift left
        const viewportWidth = window.innerWidth || document.documentElement.clientWidth;
        if (left + desiredWidth + 12 > viewportWidth) {
            left = Math.max(8, viewportWidth - desiredWidth - 12);
        }

        // If not enough space below, show above the input
        const viewportHeight = window.innerHeight || document.documentElement.clientHeight;
        const estimatedHeight = panel.offsetHeight || 200;
        if (top + estimatedHeight + 12 > viewportHeight) {
            top = rect.top - spacing - estimatedHeight;
            if (top < 8) top = rect.bottom + spacing; // fallback to below
        }

        panel.style.left = left + 'px';
        panel.style.top = top + 'px';
        panel.style.width = desiredWidth + 'px';
    }

    function renderResults(filtered) {
        if (!panel) return;
        if (!filtered || filtered.length === 0) {
            panel.innerHTML = '<div class="p-3 text-sm text-gray-500 dark:text-gray-400">Brak wyników</div>';
            return;
        }
        let html = '';
        filtered.forEach(it => {
            if (it.type === 'category') {
                html += `<div class="px-3 py-2 font-semibold text-sm text-gray-700 dark:text-gray-200 border-b border-gray-100 dark:border-gray-800">${escapeHtml(it.name)}</div>`;
            } else {
                html += `<a href="/forums.html?forumId=${it.id}" class="block px-3 py-2 text-sm text-gray-700 dark:text-gray-200 hover:bg-gray-100 dark:hover:bg-[#232f48]">${escapeHtml(it.name)} <span class="text-xs text-gray-400">· ${escapeHtml(it.categoryName||'')}</span></a>`;
            }
        });
        panel.innerHTML = html;
    }

    const doFilter = debounce(async function(e) {
        await loadItems();
        const q = (e.target.value || '').toLowerCase().trim();
        const filtered = items.filter(it => it.name && it.name.toLowerCase().includes(q));
        renderResults(filtered);
        positionPanel();
        panel.style.display = 'block';
    }, 150);

    input.addEventListener('input', function(e) {
        doFilter(e);
    });

    input.addEventListener('focus', async function() {
        await loadItems();
        renderResults(items);
        positionPanel();
        panel.style.display = 'block';
    });

    const toggle = document.getElementById('search-toggle');
    if (toggle) toggle.addEventListener('click', async function(e) {
        e.preventDefault();
        // ensure the input container is visible (some pages hide it)
        container.classList.remove('hidden');
        container.classList.add('flex');
        await loadItems();
        renderResults(items);
        positionPanel();
        panel.style.display = 'block';
        input.focus();
    });

    closeBtn?.addEventListener('click', function() {
        panel.style.display = 'none';
        input.value = '';
        // hide container to match forum.html behavior
        container.classList.add('hidden');
        container.classList.remove('flex');
    });

    window.addEventListener('resize', function() { if (panel.style.display !== 'none') positionPanel(); });
    window.addEventListener('scroll', function() { if (panel.style.display !== 'none') positionPanel(); }, true);

    document.addEventListener('click', function(e) {
        if (e.target === input || panel.contains(e.target) || container.contains(e.target)) return;
        panel.style.display = 'none';
    });

    // close panel when clicking a link inside
    panel.addEventListener('click', function(e) {
        const a = e.target.closest('a');
        if (a) panel.style.display = 'none';
    });
}

function debounce(func, wait) {
    let timeout;
    return function executedFunction(...args) {
        const later = () => {
            clearTimeout(timeout);
            func(...args);
        };
        clearTimeout(timeout);
        timeout = setTimeout(later, wait);
    };
}

function performSearch(query) {
    console.log('🔍 Wyszukiwanie:', query);
}

function setupThemeToggle() {
    const savedTheme = localStorage.getItem('theme') || 'dark';
    applyTheme(savedTheme);
}

function applyTheme(theme) {
    const html = document.documentElement;
    
    if (theme === 'dark') {
        html.classList.add('dark');
    } else {
        html.classList.remove('dark');
    }
    
    localStorage.setItem('theme', theme);
}

function setupForumInteractions() {
    const forumThreads = document.querySelectorAll('.forum-thread');

    forumThreads.forEach(thread => {
        thread.classList.add('forum-thread-hover');

        thread.addEventListener('click', function(e) {
            // If the clicked element is an anchor (or inside one), force navigation
            const anchor = e.target.closest('a');
            if (anchor && anchor.href) {
                window.location.href = anchor.href;
                return;
            }

            // Try to find an <a> inside the thread card
            const link = thread.querySelector('a');
            if (link && link.href) {
                window.location.href = link.href;
                return;
            }

            // If there's no anchor, try to use data-forum-id attribute
            const forumId = thread.getAttribute('data-forum-id');
            if (forumId) {
                window.location.href = `forum.html?forumId=${encodeURIComponent(forumId)}`;
            }
        });
    });
}

const Utils = {
    formatNumber: function(num) {
        return num.toString().replace(/\B(?=(\d{3})+(?!\d))/g, ',');
    },
    
    timeAgo: function(date) {
        const now = new Date();
        const diffInMs = now - date;
        const diffInMinutes = Math.floor(diffInMs / (1000 * 60));
        
        if (diffInMinutes < 1) return 'teraz';
        if (diffInMinutes < 60) return `${diffInMinutes} min temu`;
        
        const diffInHours = Math.floor(diffInMinutes / 60);
        if (diffInHours < 24) return `${diffInHours} godz temu`;
        
        const diffInDays = Math.floor(diffInHours / 24);
        return `${diffInDays} dni temu`;
    },
    
    showLoading: function(element) {
        element.classList.add('loading');
    },
    
    hideLoading: function(element) {
        element.classList.remove('loading');
    }
};

// Funkcjonalność uwierzytelniania i UI
async function setupAuthenticationUI() {
    console.log('🔐 Konfigurowanie uwierzytelniania UI...');

    try {
        const currentUser = await getCurrentUser();
        updateTopBarUI(currentUser);

        if (currentUser) {
            setTimeout(() => setupAvatarDropdown(), 50);
        }
    } catch (error) {
        console.error('❌ Błąd konfigurowania uwierzytelniania UI:', error);
    }
}
async function apiFetch(url, options = {}) {
    options = Object.assign({
        credentials: 'include',
        headers: { 'Accept': 'application/json' }
    }, options || {});

    try {
        const res = await fetch(url, options);
        // 403 = użytkownik zablokowany - wyloguj
        if (res.status === 403 && url.includes('/api/')) {
            const data = await res.json();
            if (data.error?.includes('zablokowane') || data.error?.includes('blocked')) {
                console.log('🚫 Konto zostało zablokowane - wylogowywanie...');
                try {
                    await fetch('/api/auth/logout', { 
                        method: 'POST',
                        credentials: 'include'
                    });
                } catch (e) {
                    console.error('Błąd wylogowywania:', e);
                }
                document.cookie = 'user_session=; expires=Thu, 01 Jan 1970 00:00:00 UTC; path=/;';
                window.location.href = '/login.html?banned=1';
            }
            return { ok: false, status: 403, json: async () => data };
        }
        // nie wyrzucamy przy 401 — zwracamy obiekt z flagą
        if (res.status === 401) {
            return { ok: false, status: 401, json: async () => (await res.text()) };
        }
        return res;
    } catch (err) {
        console.error('apiFetch error:', err);
        throw err;
    }
}
async function getCurrentUser() {
    try {
        console.log('🔍 Sprawdzanie statusu uwierzytelniania...');
        
        const response = await fetch('/api/auth/status', {
            method: 'GET',
            credentials: 'include', // ✅ WAŻNE: wysyłaj cookies
            headers: {
                'Content-Type': 'application/json',
                'Accept': 'application/json'
            }
        });

        console.log('📡 Response status:', response.status);

        // 403 = użytkownik zablokowany
        if (response.status === 403) {
            console.log('🚫 Użytkownik zablokowany! Wylogowywanie...');
            try {
                await fetch('/api/auth/logout', { 
                    method: 'POST',
                    credentials: 'include'
                });
            } catch (e) {
                console.error('Błąd wylogowywania:', e);
            }
            // Wyczyść sesję
            document.cookie = 'user_session=; expires=Thu, 01 Jan 1970 00:00:00 UTC; path=/;';
            window.location.href = '/login.html?banned=1';
            return null;
        }

        // 401 = brak autentykacji (normalne przy pierwszej wizycie)
        if (response.status === 401) {
            console.log('ℹ️ Użytkownik niezalogowany (status: 401)');
            return null;
        }

        if (!response.ok) {
            console.log('⚠️ Błąd odpowiedzi:', response.status);
            return null;
        }

        const userData = await response.json();
        console.log('✅ Użytkownik zalogowany:', userData);
        return userData;

    } catch (error) {
        console.error('❌ Błąd sprawdzania statusu:', error);
        return null;
    }
}

function updateTopBarUI(currentUser) {
    console.log('🎨 Aktualizowanie UI dla użytkownika:', currentUser);
    const authContainer = document.getElementById('auth-container');
    
    if (!authContainer) {
        console.log('⚠️ Brak elementu #auth-container');
        return;
    }
    
    // Jeśli jesteśmy na stronie profilu, wymuszaj stan zalogowany
    if (window.location.pathname.includes('profile.html') && !currentUser) {
        // Symuluj użytkownika dla profilu
        currentUser = {
            username: 'NazwaUzytkownika',
            avatar: 'https://lh3.googleusercontent.com/aida-public/AB6AXuDPDRVHwPah9K-9R_UMO08XQt1xmxrlcMTF_2uhXTkStZypenzqhB4ag3pXxFAIwxVcEGkqHaJkgHLPaXyKNNWrNW_XoYOEZ_0VKyv4COUc2_nxfRm3xJwkTlRMEqjQMuQolpEOgaotsSc4g6Z11zsKEwxJ0f_kafyVGOBjgQeQicLMROyT1exorutkxj73H6wl6jD2C6dCkDJ_cnEr9lfJDOfE5ptWoy3Kw3Iszo-BDE4wDow-JSYAPVpnCwx4yv4QUUn982MFCYvT'
        };
    }
    
    // Jeśli kontener jest pusty, wypełnij go odpowiednim UI
    if (!authContainer.innerHTML.trim()) {
        if (!currentUser) {
            authContainer.innerHTML = `
                <div class="flex space-x-4">
                    <a href="/login.html" class="bg-white text-blue-700 px-4 py-2 rounded hover:bg-gray-100 font-medium">Logowanie</a>
                    <a href="/register.html" class="bg-blue-600 text-white px-4 py-2 rounded hover:bg-blue-700 font-medium">Rejestracja</a>
                </div>
            `;
            return;
        }

        // Zalogowany - wstaw avatar + menu
        const userInitial = (currentUser.username || 'U').charAt(0).toUpperCase();
        authContainer.innerHTML = `
            <div class="relative">
                <button id="avatar-btn" class="js-avatar-btn w-10 h-10 bg-blue-500 text-white rounded-full hover:ring-2 hover:ring-blue-400/50 transition-all flex items-center justify-center font-semibold text-lg cursor-pointer">${userInitial}</button>
                <div id="avatar-menu" class="js-avatar-menu hidden absolute right-0 top-full mt-2 w-48 bg-white dark:bg-[#192231] rounded-lg shadow-lg border border-gray-200 dark:border-gray-700 py-2 z-50">
                    <div class="px-4 py-3 border-b border-gray-200 dark:border-gray-700">
                        <p class="text-sm font-semibold text-gray-900 dark:text-white">${escapeHtml(currentUser.username)}</p>
                        <p class="text-xs text-gray-500 dark:text-gray-400">${escapeHtml(currentUser.email || 'Brak e-maila')}</p>
                    </div>
                    <a href="/profile.html" class="block px-4 py-2 text-sm text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-[#2a3851]">Mój profil</a>
                    <a href="/messages.html" class="block px-4 py-2 text-sm text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-[#2a3851]">Wiadomości</a>
                    <hr class="my-1 border-gray-200 dark:border-gray-700">
                    <button id="logout-btn" class="w-full text-left px-4 py-2 text-sm text-red-600 dark:text-red-400 hover:bg-red-50 dark:hover:bg-red-900/20">Wyloguj się</button>
                </div>
            </div>
        `;

        // Podłącz eventy
        const avatarBtn = authContainer.querySelector('.js-avatar-btn') || document.getElementById('avatar-btn');
        const avatarMenu = authContainer.querySelector('.js-avatar-menu') || document.getElementById('avatar-menu');
        const logoutBtn = authContainer.querySelector('#logout-btn') || document.getElementById('logout-btn');
        if (avatarBtn && avatarMenu) {
            avatarBtn.addEventListener('click', () => avatarMenu.classList.toggle('hidden'));
            document.addEventListener('click', (e) => {
                if (!avatarBtn.contains(e.target) && !avatarMenu.contains(e.target)) avatarMenu.classList.add('hidden');
            });
        }
        if (logoutBtn) {
            logoutBtn.addEventListener('click', async () => {
                try {
                    await fetch('/api/auth/logout', { method: 'POST', credentials: 'include' });
                    window.location.href = '/login.html';
                } catch (err) { console.error('Logout error', err); }
            });
        }
        return;
    }

    // Jeżeli kontener miał zawartość, tylko zaktualizuj avatar (jeśli mamy dane)
    if (currentUser && currentUser.avatar) {
    const avatarElement = authContainer.querySelector('[style*="background-image"]') || authContainer.querySelector('.js-avatar-btn, #avatar-btn');
        if (avatarElement) {
            try { avatarElement.style.backgroundImage = `url('${currentUser.avatar}')`; } catch(e){}
        }
    }
}

function setupAvatarDropdown() {
    console.log('🔽 Konfigurowanie dropdown avatara...');
    // Support both legacy IDs and the new class-based selectors
    const trigger = document.querySelector('.js-avatar-btn, #avatar-dropdown-trigger, #avatar-btn');
    const menu = document.querySelector('.js-avatar-menu, #avatar-dropdown-menu, #avatar-menu');

    console.log('🔍 Dropdown elementy - trigger:', !!trigger, 'menu:', !!menu);

    if (!trigger || !menu) {
        console.log('⚠️ Nie znaleziono elementów dropdown');
        return;
    }

    // Avoid binding handlers multiple times
    if (trigger.dataset.dropdownInitialized) {
        console.log('ℹ️ Dropdown już zainicjalizowany');
        return;
    }

    trigger.dataset.dropdownInitialized = '1';
    console.log('✅ Dropdown skonfigurowany');

    trigger.addEventListener('click', function(e) {
        e.stopPropagation();
        e.preventDefault();
        menu.classList.toggle('hidden');
    });

    // Zamknij menu po kliknięciu poza nim
    document.addEventListener('click', function(e) {
        if (!trigger.contains(e.target) && !menu.contains(e.target)) {
            menu.classList.add('hidden');
        }
    });

    // Zamknij menu po naciśnięciu Escape
    document.addEventListener('keydown', function(e) {
        if (e.key === 'Escape') {
            menu.classList.add('hidden');
        }
    });
}

// Delegowany handler dla avatara - bezpieczny niezależnie od tego, kiedy HTML jest wstawiony
function setupAvatarDelegation() {
    if (_avatarDelegationSetup) return; // Prevent duplicate listeners
    _avatarDelegationSetup = true;
    
    document.addEventListener('click', function(e) {
        // Kliknięcie w przycisk avatara
        const btn = e.target.closest('.js-avatar-btn, #avatar-btn');
        if (btn) {
            e.stopPropagation();
            e.preventDefault();
            const wrapper = btn.closest('div');
            if (!wrapper) return;
            const menu = wrapper.querySelector('.js-avatar-menu, #avatar-menu');
            if (menu) menu.classList.toggle('hidden');
            return;
        }

        // Kliknięcie poza -- zamknij wszystkie otwarte menu avatara
        document.querySelectorAll('.js-avatar-menu, #avatar-menu').forEach(menu => {
            if (!menu.classList.contains('hidden') && !menu.contains(e.target)) {
                menu.classList.add('hidden');
            }
        });
    });
    // Obsługa Esc globalnie
    document.addEventListener('keydown', function(e) {
        if (e.key === 'Escape') {
            document.querySelectorAll('.js-avatar-menu, #avatar-menu').forEach(m => m.classList.add('hidden'));
        }
    });
}

// Additionally listen on pointerdown with capture to catch interactions earlier
// DISABLED: This was causing issues where menu closes immediately
/*
function setupAvatarDelegationCapture() {
    document.addEventListener('pointerdown', function(e) {
        const btn = e.target.closest('.js-avatar-btn, #avatar-btn');
        if (btn) {
            const wrapper = btn.closest('div');
            if (!wrapper) return;
            const menu = wrapper.querySelector('.js-avatar-menu, #avatar-menu');
            if (menu) {
                // If menu is closed, open it. If open, close it here to provide immediate response
                if (menu.classList.contains('hidden')) {
                    menu.classList.remove('hidden');
                } else {
                    menu.classList.add('hidden');
                }
                // mark that pointerdown toggled the menu so the following click doesn't toggle again
                try { btn.dataset._justToggled = String(Date.now()); } catch (err) {}
                e.stopPropagation();
            }
        }
    }, { capture: true });
}
*/

// Ensure the capture-based delegation is installed once
// try { setupAvatarDelegationCapture(); } catch (e) { /* ignore */ }

// Dodatkowe zabezpieczenie: podłącz bezpośrednio listenery do istniejących przycisków avatara
function ensureAvatarButtons() {
    document.querySelectorAll('.js-avatar-btn, #avatar-btn').forEach(btn => {
        // unikaj wielokrotnego podpinania
        if (btn.dataset._avatarHandlerAttached) return;
        btn.dataset._avatarHandlerAttached = '1';

        btn.addEventListener('click', function(e) {
            // znajdź powiązane menu (w najbliższym wrapperze)
            const wrapper = btn.closest('div');
            if (!wrapper) return;
            const menu = wrapper.querySelector('.js-avatar-menu, #avatar-menu');
            if (menu) menu.classList.toggle('hidden');
            e.stopPropagation();
        });
    });
}

// Wywołaj ensureAvatarButtons co jakiś czas przez krótki okres, aby złapać dynamiczne wstawienia
setTimeout(ensureAvatarButtons, 50);
setTimeout(ensureAvatarButtons, 250);
setTimeout(ensureAvatarButtons, 1000);

async function logout() {
    try {
        console.log('🚪 Wylogowywanie...');
        const response = await fetch('/api/auth/logout', {
            method: 'POST',
            credentials: 'include'
        });
        
        if (response.ok) {
            console.log('✅ Wylogowano pomyślnie');
            window.location.href = '/';
        } else {
            console.error('❌ Błąd wylogowania');
            // Wyloguj lokalnie nawet jeśli API nie odpowiada
            window.location.href = '/';
        }
    } catch (error) {
        console.error('❌ Błąd połączenia podczas wylogowania:', error);
        // Wyloguj lokalnie nawet jeśli API nie odpowiada
        window.location.href = '/';
    }
}

window.escapeHtml = function(text) {
    if (!text) return '';
    return String(text).replace(/[&<>"']/g, m => ({
        '&': '&amp;',
        '<': '&lt;',
        '>': '&gt;',
        '"': '&quot;',
        "'": '&#039;'
    })[m]);
};

window.fetchJson = async function(url, opts = {}) {
    try {
        const res = await fetch(url, Object.assign({
            credentials: 'include',
            headers: { 'Accept': 'application/json' }
        }, opts));
        if (!res.ok) return { ok: false, status: res.status, json: null };
        const json = await res.json();
        return { ok: true, status: res.status, json };
    } catch (e) {
        console.warn('fetchJson error', url, e);
        return { ok: false, status: 0, json: null };
    }
};

// API
const API = {
    baseURL: '/api',
    
    getStatus: async function() {
        try {
            const response = await fetch(`${this.baseURL}/status`);
            return await response.json();
        } catch (error) {
            console.error('❌ Błąd API:', error);
            return null;
        }
    },
    
    search: async function(query) {
        try {
            const response = await fetch(`${this.baseURL}/search?q=${encodeURIComponent(query)}`);
            return await response.json();
        } catch (error) {
            console.error('❌ Błąd wyszukiwania:', error);
            return [];
        }
    }
};
window.ForumApp = {
    Utils,
    API,
    applyTheme,
    getCurrentUser,
    updateTopBarUI,
    setupAuthenticationUI,
    logout
};
