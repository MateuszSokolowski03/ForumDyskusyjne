document.addEventListener('DOMContentLoaded', function() {
    initializeApp();
});

function initializeApp() {
    setupSearchFunctionality();
    setupThemeToggle();
    setupForumInteractions();
    setupAuthenticationUI();
    
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
    const forumThreads = document.querySelectorAll('[class*="hover:bg-gray"]');
    
    forumThreads.forEach(thread => {
        thread.classList.add('forum-thread-hover');
        
        thread.addEventListener('click', function(e) {
            if (e.target.tagName === 'A') return;
            
            const link = thread.querySelector('a');
            if (link) {
                console.log('📝 Otwieranie wątku:', link.textContent);
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
        console.log('👤 Aktualny użytkownik:', currentUser);
        updateTopBarUI(currentUser);
        
        if (currentUser) {
            // Małe opóźnienie tylko dla dropdown żeby DOM się zaktualizował
            setTimeout(() => {
                setupAvatarDropdown();
            }, 50);
        }
    } catch (error) {
        console.error('❌ Błąd konfigurowania uwierzytelniania UI:', error);
    }
}

async function getCurrentUser() {
    try {
        console.log('🔍 Sprawdzanie statusu uwierzytelniania...');
        // Sprawdź status uwierzytelniania z serwera
        const response = await fetch('/api/auth/status', {
            method: 'GET',
            credentials: 'include' // Dołącz cookies
        });
        
        console.log('📡 Response status:', response.status);
        
        if (response.ok) {
            const userData = await response.json();
            console.log('✅ Użytkownik zalogowany:', userData);
            return userData;
        } else {
            console.log('ℹ️ Użytkownik niezalogowany (status:', response.status, ')');
            return null;
        }
    } catch (error) {
        console.error('❌ Błąd sprawdzania statusu uwierzytelniania:', error);
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
    
    // Sprawdź czy inline script już ustawił zawartość
    if (authContainer.innerHTML.trim() && currentUser) {
        // Tylko zaktualizuj avatar jeśli już jest zawartość i mamy dane użytkownika
        const avatarElement = authContainer.querySelector('[style*="background-image"]');
        if (avatarElement && currentUser.avatar) {
            avatarElement.style.backgroundImage = `url('${currentUser.avatar}')`;
        }
        return;
    }
    
    // Inline script w head już ustawił podstawową zawartość
    // Ta funkcja tylko aktualizuje avatar gdy otrzymamy lepsze dane z API
    if (currentUser && currentUser.avatar) {
        const avatarElement = authContainer.querySelector('[style*="background-image"]');
        if (avatarElement) {
            avatarElement.style.backgroundImage = `url('${currentUser.avatar}')`;
        }
    }
}

function setupAvatarDropdown() {
    console.log('🔽 Konfigurowanie dropdown avatara...');
    const trigger = document.getElementById('avatar-dropdown-trigger');
    const menu = document.getElementById('avatar-dropdown-menu');
    
    console.log('🔍 Dropdown elementy - trigger:', !!trigger, 'menu:', !!menu);
    
    if (!trigger || !menu) {
        console.log('⚠️ Nie znaleziono elementów dropdown');
        return;
    }
    
    console.log('✅ Dropdown skonfigurowany');
    
    trigger.addEventListener('click', function(e) {
        e.stopPropagation();
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
