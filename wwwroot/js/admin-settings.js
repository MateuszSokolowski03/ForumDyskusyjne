// Admin Settings Management

class AdminSettings {
    constructor() {
        this.bannedWords = [];
        this.init();
    }

    init() {
        this.setupEventListeners();
        this.loadBannedWords();
    }

    setupEventListeners() {
        // Banned words form
        const bannedWordsForm = document.getElementById('bannedWordsForm');
        if (bannedWordsForm) {
            bannedWordsForm.addEventListener('submit', (e) => {
                e.preventDefault();
                this.addBannedWord();
            });
        }

        // User Management - Manage Ranks
        const manageRanksBtn = document.getElementById('manage-ranks-btn');
        if (manageRanksBtn) {
            manageRanksBtn.addEventListener('click', () => {
                this.showRanksModal();
            });
        }

        // Categories Management - Manage Structure
        const manageCategoriesBtn = document.getElementById('manage-categories-btn');
        if (manageCategoriesBtn) {
            manageCategoriesBtn.addEventListener('click', () => {
                window.location.href = '/admin/categories';
            });
        }

        // Categories Management - Manage Permissions
        const managePermissionsBtn = document.getElementById('manage-permissions-btn');
        if (managePermissionsBtn) {
            managePermissionsBtn.addEventListener('click', () => {
                alert('Funkcja zarządzania uprawnieniami gości będzie dostępna wkrótce');
            });
        }

        // Other Settings - File Limits
        const manageLimitsBtn = document.getElementById('manage-limits-btn');
        if (manageLimitsBtn) {
            manageLimitsBtn.addEventListener('click', () => {
                alert('Funkcja zarządzania limitami plików będzie dostępna wkrótce');
            });
        }

        // Other Settings - HTML Tags
        const manageHtmlBtn = document.getElementById('manage-html-btn');
        if (manageHtmlBtn) {
            manageHtmlBtn.addEventListener('click', () => {
                alert('Funkcja zarządzania tagami HTML będzie dostępna wkrótce');
            });
        }

        // Other Settings - Themes and Languages
        const manageThemesBtn = document.getElementById('manage-themes-btn');
        if (manageThemesBtn) {
            manageThemesBtn.addEventListener('click', () => {
                alert('Funkcja zarządzania skórkami będzie dostępna wkrótce');
            });
        }
    }

    async loadBannedWords() {
        try {
            const response = await fetch('/api/admin/banned-words');
            if (response.ok) {
                const words = await response.json();
                this.bannedWords = words;
                this.renderBannedWords();
            }
        } catch (error) {
            console.error('Error loading banned words:', error);
        }
    }

    renderBannedWords() {
        const container = document.getElementById('bannedWordsList');
        if (!container) return;

        if (this.bannedWords.length === 0) {
            container.innerHTML = '<p class="text-gray-500 dark:text-gray-400 text-sm">Brak zablokowanych słów</p>';
            return;
        }

        container.innerHTML = this.bannedWords.map(word => `
            <span class="tag">
                ${word}
                <button class="tag-remove" onclick="adminSettings.removeBannedWord('${word}')">
                    <span class="material-symbols-outlined !text-base">close</span>
                </button>
            </span>
        `).join('');
    }

    async addBannedWord() {
        const input = document.getElementById('banned-word');
        const word = input.value.trim();

        if (!word) {
            window.adminPanel.showError('Wprowadź słowo do zablokowania');
            return;
        }

        if (this.bannedWords.includes(word)) {
            window.adminPanel.showError('To słowo jest już na liście');
            return;
        }

        try {
            const response = await fetch('/api/admin/banned-words', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ word })
            });

            if (response.ok) {
                this.bannedWords.push(word);
                this.renderBannedWords();
                input.value = '';
                window.adminPanel.showSuccess('Słowo zostało dodane do listy zakazanych');
            } else {
                const error = await response.text();
                window.adminPanel.showError(`Błąd: ${error}`);
            }
        } catch (error) {
            window.adminPanel.showError('Wystąpił błąd podczas dodawania słowa');
        }
    }

    async removeBannedWord(word) {
        if (!confirm(`Czy na pewno chcesz usunąć słowo "${word}" z listy zakazanych?`)) {
            return;
        }

        try {
            const response = await fetch(`/api/admin/banned-words/${encodeURIComponent(word)}`, {
                method: 'DELETE'
            });

            if (response.ok) {
                this.bannedWords = this.bannedWords.filter(w => w !== word);
                this.renderBannedWords();
                window.adminPanel.showSuccess('Słowo zostało usunięte z listy zakazanych');
            } else {
                const error = await response.text();
                window.adminPanel.showError(`Błąd: ${error}`);
            }
        } catch (error) {
            window.adminPanel.showError('Wystąpił błąd podczas usuwania słowa');
        }
    }

    showRanksModal() {
        const modal = document.createElement('div');
        modal.id = 'ranks-modal';
        modal.className = 'fixed inset-0 bg-black/50 z-50 flex items-center justify-center p-4';
        modal.innerHTML = `
            <div class="bg-white dark:bg-[#192231] rounded-lg shadow-lg max-w-2xl w-full p-6 max-h-96 overflow-y-auto">
                <div class="flex items-center justify-between mb-4">
                    <h2 class="text-xl font-bold text-gray-900 dark:text-white">Zarządzanie Rangami Użytkowników</h2>
                    <button class="text-gray-500 hover:text-gray-700 dark:text-gray-400 dark:hover:text-gray-200" onclick="document.getElementById('ranks-modal')?.remove()">
                        <span class="material-symbols-outlined">close</span>
                    </button>
                </div>
                <div id="ranks-list" class="space-y-3">
                    <p class="text-center text-gray-500 dark:text-gray-400 py-8">Ładowanie rang...</p>
                </div>
            </div>
        `;
        document.body.appendChild(modal);

        modal.addEventListener('click', (e) => {
            if (e.target === modal) modal.remove();
        });

        this.loadRanks();
    }

    async loadRanks() {
        try {
            const response = await fetch('/api/users/ranks');
            if (response.ok) {
                const ranks = await response.json();
                this.displayRanks(ranks);
            } else {
                document.getElementById('ranks-list').innerHTML = '<p class="text-red-500">Błąd ładowania rang</p>';
            }
        } catch (error) {
            console.error('Error loading ranks:', error);
            document.getElementById('ranks-list').innerHTML = '<p class="text-red-500">Błąd: ' + error.message + '</p>';
        }
    }

    displayRanks(ranks) {
        const container = document.getElementById('ranks-list');
        if (!Array.isArray(ranks) || ranks.length === 0) {
            container.innerHTML = '<p class="text-gray-500 dark:text-gray-400">Brak dostępnych rang</p>';
            return;
        }

        container.innerHTML = ranks.map(rank => `
            <div class="p-3 bg-gray-50 dark:bg-[#232f48] rounded-lg border border-gray-200 dark:border-[#324467]">
                <div class="flex items-center justify-between">
                    <div>
                        <p class="font-semibold text-gray-900 dark:text-white">${rank.name || 'Brak nazwy'}</p>
                        <p class="text-sm text-gray-500 dark:text-gray-400">Minimalna liczba wiadomości: ${rank.minMessages || 0}</p>
                    </div>
                    <button class="px-3 py-1 bg-primary hover:bg-primary/90 text-white rounded text-sm transition" onclick="alert('Funkcja edycji będzie dostępna wkrótce')">
                        Edytuj
                    </button>
                </div>
            </div>
        `).join('');
    }
}

// Global functions for onclick handlers
function removeBannedWord(word) {
    if (window.adminSettings) {
        window.adminSettings.removeBannedWord(word);
    }
}

// Initialize
document.addEventListener('DOMContentLoaded', () => {
    window.adminSettings = new AdminSettings();
});
