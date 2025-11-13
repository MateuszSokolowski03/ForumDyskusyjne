document.addEventListener('DOMContentLoaded', function() {
    initializeApp();
});

function initializeApp() {
    setupSearchFunctionality();
    setupThemeToggle();
    setupForumInteractions();
    
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
    applyTheme
};
