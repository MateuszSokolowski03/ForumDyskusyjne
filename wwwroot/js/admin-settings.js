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
