// Admin Panel JavaScript

class AdminPanel {
    constructor() {
        this.init();
    }

    init() {
        this.setupEventListeners();
        this.loadStats();
        this.setupNavigation();
    }

    setupEventListeners() {
        // Mobile menu toggle
        const menuToggle = document.querySelector('.menu-toggle');
        const sidebar = document.querySelector('.admin-sidebar');
        
        if (menuToggle) {
            menuToggle.addEventListener('click', () => {
                sidebar.classList.toggle('open');
            });
        }

        // Close sidebar when clicking outside on mobile
        document.addEventListener('click', (e) => {
            if (window.innerWidth <= 768) {
                if (!sidebar.contains(e.target) && !menuToggle?.contains(e.target)) {
                    sidebar.classList.remove('open');
                }
            }
        });

        // Logout confirmation
        const logoutBtn = document.querySelector('.logout-btn');
        if (logoutBtn) {
            logoutBtn.addEventListener('click', (e) => {
                e.preventDefault();
                this.confirmLogout();
            });
        }
    }

    setupNavigation() {
        const navItems = document.querySelectorAll('.nav-item');
        const currentPath = window.location.pathname;

        navItems.forEach(item => {
            const href = item.getAttribute('href');
            if (href === currentPath || (currentPath.includes('/admin') && href.includes(currentPath.split('/').pop()))) {
                item.classList.add('active');
            } else {
                item.classList.remove('active');
            }
        });
    }

    async loadStats() {
        try {
            const response = await fetch('/api/admin/stats');
            if (response.ok) {
                const stats = await response.json();
                console.log('Statystyki z API:', stats);
                this.updateStatsCards(stats);
            } else {
                throw new Error(`HTTP ${response.status}`);
            }
        } catch (error) {
            console.error('Błąd ładowania statystyk:', error);
            // Pokazuj placeholder values podczas błędu
            this.updateStatsCards({
                users: 0,
                threads: 0,
                messages: 0,
                categories: 0
            });
        }
    }

    updateStatsCards(stats) {
        // Znajdź karty statystyk po kolejności (bardziej niezawodne)
        const statCards = document.querySelectorAll('.stat-card .stat-value');
        
        if (statCards.length >= 4) {
            statCards[0].textContent = this.formatNumber(stats.users || 0);      // Użytkownicy
            statCards[1].textContent = this.formatNumber(stats.threads || 0);   // Wątki
            statCards[2].textContent = this.formatNumber(stats.messages || 0);  // Posty/Messages
            statCards[3].textContent = this.formatNumber(stats.categories || 0); // Kategorie
        }
    }

    formatNumber(num) {
        return new Intl.NumberFormat('pl-PL').format(num);
    }

    confirmLogout() {
        if (confirm('Czy na pewno chcesz się wylogować?')) {
            window.location.href = '/';
        }
    }

    // Utility functions for other admin pages
    showLoading(element) {
        element.classList.add('loading');
        const spinner = document.createElement('div');
        spinner.className = 'spinner';
        element.appendChild(spinner);
    }

    hideLoading(element) {
        element.classList.remove('loading');
        const spinner = element.querySelector('.spinner');
        if (spinner) {
            spinner.remove();
        }
    }

    showModal(title, content, buttons = []) {
        const modal = document.createElement('div');
        modal.className = 'modal-overlay';
        modal.innerHTML = `
            <div class="modal-content">
                <h3 class="text-xl font-bold mb-4">${title}</h3>
                <div class="modal-body mb-6">${content}</div>
                <div class="modal-buttons flex gap-2 justify-end">
                    ${buttons.map(btn => `
                        <button class="btn-${btn.type || 'secondary'}" onclick="${btn.action || 'this.closest(\'.modal-overlay\').remove()'}">${btn.text}</button>
                    `).join('')}
                </div>
            </div>
        `;
        document.body.appendChild(modal);

        // Close on overlay click
        modal.addEventListener('click', (e) => {
            if (e.target === modal) {
                modal.remove();
            }
        });

        return modal;
    }

    showSuccess(message) {
        this.showNotification(message, 'success');
    }

    showError(message) {
        this.showNotification(message, 'error');
    }

    showNotification(message, type = 'info') {
        const notification = document.createElement('div');
        notification.className = `notification notification-${type} fixed top-4 right-4 p-4 rounded-lg shadow-lg z-50 transform translate-x-full transition-transform duration-300`;
        
        const colors = {
            success: 'bg-green-500 text-white',
            error: 'bg-red-500 text-white',
            info: 'bg-blue-500 text-white',
            warning: 'bg-yellow-500 text-black'
        };

        notification.classList.add(...colors[type].split(' '));
        notification.textContent = message;

        document.body.appendChild(notification);

        // Animate in
        setTimeout(() => {
            notification.classList.remove('translate-x-full');
        }, 100);

        // Auto remove
        setTimeout(() => {
            notification.classList.add('translate-x-full');
            setTimeout(() => {
                notification.remove();
            }, 300);
        }, 3000);
    }
}

// Initialize admin panel
document.addEventListener('DOMContentLoaded', () => {
    window.adminPanel = new AdminPanel();
});

// Export for use in other files
if (typeof module !== 'undefined' && module.exports) {
    module.exports = AdminPanel;
}
