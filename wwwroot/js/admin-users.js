// Admin Users Management

class AdminUsers {
    constructor() {
        this.currentPage = 1;
        this.pageSize = 10;
        this.totalUsers = 0;
        this.allUsers = []; 
        this.availableRanks = []; // Cache dostępnych rang
        this.selectedUserId = null; // ID użytkownika edytowanego w modalu rang
        this.filters = {
            search: '',
            role: '',
            status: '',
            rankId: '' // Nowy filtr
        };
        this.init();
    }

    init() {
        this.setupEventListeners();
        this.loadUsers();
        this.loadRanks(); // Pobierz rangi przy starcie
    }

    setupEventListeners() {
        const searchInput = document.getElementById('search-users');
        if (searchInput) {
            searchInput.addEventListener('input', (e) => {
                this.filters.search = e.target.value;
                this.debounceSearch();
            });
        }

        // Automatyczne odświeżanie przy zmianie filtrów
        const roleFilter = document.getElementById('filter-role');
        const statusFilter = document.getElementById('filter-status');
        const rankFilter = document.getElementById('filter-rank');

        if (roleFilter) {
            roleFilter.addEventListener('change', (e) => {
                this.filters.role = e.target.value;
                this.loadUsers();
            });
        }

        if (statusFilter) {
            statusFilter.addEventListener('change', (e) => {
                this.filters.status = e.target.value;
                this.loadUsers();
            });
        }

        if (rankFilter) {
            rankFilter.addEventListener('change', (e) => {
                this.filters.rankId = e.target.value; // Zapisz wybraną rangę
                this.loadUsers(); // Odśwież tabelę
            });
        }

        const prevBtn = document.getElementById('prev-page');
        const nextBtn = document.getElementById('next-page');

        if (prevBtn) {
            prevBtn.addEventListener('click', () => {
                if (this.currentPage > 1) {
                    this.currentPage--;
                    this.loadUsers();
                }
            });
        }

        if (nextBtn) {
            nextBtn.addEventListener('click', () => {
                const totalPages = Math.ceil(this.totalUsers / this.pageSize);
                if (this.currentPage < totalPages) {
                    this.currentPage++;
                    this.loadUsers();
                }
            });
        }
    }

    debounceSearch() {
        clearTimeout(this.searchTimeout);
        this.searchTimeout = setTimeout(() => {
            this.currentPage = 1;
            this.loadUsers();
        }, 500);
    }

    // --- Pobieranie Rang i wypełnianie filtra ---
    async loadRanks() {
        try {
            const response = await fetch('/api/users/ranks', { credentials: 'include' });
            if (response.ok) {
                this.availableRanks = await response.json();
                
                // Wypełnij nowy select filtra w HTML
                const filterSelect = document.getElementById('filter-rank');
                if (filterSelect) {
                    // Zachowaj pierwszą opcję "Wszystkie rangi" i dodaj resztę
                    filterSelect.innerHTML = '<option value="">Wszystkie rangi</option>';
                    
                    this.availableRanks.forEach(rank => {
                        filterSelect.innerHTML += `<option value="${rank.id}">${rank.name}</option>`;
                    });
                }
            }
        } catch (error) {
            console.error('Błąd pobierania rang:', error);
        }
    }

    async loadUsers() {
        const tableBody = document.getElementById('users-table-body');
        if (!tableBody) return;

        // Ustawienie statusu ładowania
        tableBody.innerHTML = `
            <tr>
                <td colspan="8" class="text-center py-8 text-gray-500 dark:text-gray-400">
                    <div class="loading-state">
                        <div class="spinner mx-auto mb-2"></div>
                        <p>Ładowanie użytkowników...</p>
                    </div>
                </td>
            </tr>
        `;

        try {
            const params = new URLSearchParams({
                search: this.filters.search,
                role: this.filters.role,
                status: this.filters.status,
                rankId: this.filters.rankId // Przekazujemy ID rangi do backendu
            });

            const response = await fetch(`/api/users?${params}`, { credentials: 'include' });
            
            if (response.ok) {
                const data = await response.json();
                
                let users = Array.isArray(data) ? data : (data.users || []);
                this.totalUsers = users.length;
                
                // Paginacja po stronie klienta
                const start = (this.currentPage - 1) * this.pageSize;
                const end = start + this.pageSize;
                const paginatedUsers = users.slice(start, end);

                this.renderUsers(paginatedUsers);
                this.updatePagination(this.totalUsers);
            } else {
                throw new Error('Błąd API: ' + response.status + ' ' + response.statusText);
            }
        } catch (error) {
            console.error('Error loading users:', error);
            tableBody.innerHTML = `
                <tr>
                    <td colspan="8" class="text-center py-8 text-red-500">
                        <p>Błąd ładowania użytkowników: ${error.message}</p>
                        <button class="text-blue-500 hover:underline mt-2" onclick="adminUsers.loadUsers()">Spróbuj ponownie</button>
                    </td>
                </tr>
            `;
        }
    }

    renderUsers(users) {
        const tableBody = document.getElementById('users-table-body');
        if (!tableBody) return;

        this.allUsers = users; 

        if (users.length === 0) {
            tableBody.innerHTML = `
                <tr>
                    <td colspan="8" class="text-center py-8 text-gray-500 dark:text-gray-400">
                        <p>Nie znaleziono użytkowników</p>
                    </td>
                </tr>
            `;
            return;
        }

        const currentUser = window.auth?.getUser();

        tableBody.innerHTML = users.map(user => {
            const isCurrentUser = currentUser && user.id === currentUser.id;
            const isAdmin = String(user.role).toLowerCase() === 'admin';
            const canBlock = !isCurrentUser && !isAdmin;

            return `
            <tr class="hover:bg-gray-50 dark:hover:bg-[#192231] border-b border-gray-100 dark:border-gray-800">
                <td class="user-info py-3 px-4">
                    <div class="flex items-center gap-3">
                        <div class="user-avatar w-10 h-10 bg-gray-200 dark:bg-gray-700 rounded-full flex items-center justify-center overflow-hidden">
                            ${user.avatarUrl ? 
                                `<img src="${user.avatarUrl}" alt="${user.username}" class="w-full h-full object-cover">` :
                                `<span class="material-symbols-outlined text-gray-500 dark:text-gray-400">person</span>`
                            }
                        </div>
                        <div>
                            <p class="font-medium text-gray-900 dark:text-white">${user.username}</p>
                            <p class="text-xs text-gray-500 dark:text-gray-400">ID: ${user.id}</p>
                        </div>
                    </div>
                </td>
                <td class="user-email py-3 px-4 text-gray-600 dark:text-gray-300 text-sm">${user.email || 'Brak email'}</td>
                <td class="user-role py-3 px-4">
                    <span class="role-badge inline-flex px-2 py-1 text-xs font-semibold rounded-full ${this.getRoleBadgeClass(user.role)}">
                        ${this.getRoleLabel(user.role)}
                    </span>
                </td>
                <td class="user-status py-3 px-4">
                    <span class="status-badge inline-flex px-2 py-1 text-xs font-semibold rounded-full ${this.getStatusBadgeClass(user.isBanned ? 'blocked' : 'active')}">
                        ${user.isBanned ? 'Zablokowany' : 'Aktywny'}
                    </span>
                </td>
                
                <td class="user-rank py-3 px-4">
                    <span class="inline-flex items-center gap-1 px-2 py-1 rounded text-xs font-medium bg-blue-50 text-blue-700 dark:bg-blue-900/20 dark:text-blue-300 border border-blue-100 dark:border-blue-800">
                        <span class="material-symbols-outlined !text-sm">military_tech</span>
                        ${user.currentRank || 'Brak'}
                    </span>
                </td>

                <td class="user-created py-3 px-4 text-gray-600 dark:text-gray-300 text-sm hidden md:table-cell">
                    ${this.formatDate(user.createdAt)}
                </td>

                <td class="user-lastlogin py-3 px-4 text-gray-600 dark:text-gray-300 text-sm hidden md:table-cell">
                    ${this.formatDate(user.lastActivityAt)}
                </td>

                <td class="user-actions py-3 px-4">
                    <div class="flex justify-center gap-2">
                        <button class="action-btn p-1.5 hover:bg-gray-100 dark:hover:bg-gray-700 rounded text-gray-500 hover:text-blue-600 dark:text-gray-400 transition-colors" 
                                onclick="adminUsers.editUser(${user.id})" 
                                title="Edytuj rolę">
                            <span class="material-symbols-outlined !text-xl">edit</span>
                        </button>
                        
                        <button class="action-btn p-1.5 hover:bg-gray-100 dark:hover:bg-gray-700 rounded text-gray-500 hover:text-amber-600 dark:text-gray-400 transition-colors" 
                                onclick="adminUsers.openRankModal(${user.id}, '${user.username}', ${user.currentRankId || 0})" 
                                title="Zmień rangę">
                            <span class="material-symbols-outlined !text-xl">military_tech</span>
                        </button>

                        ${!user.isBanned ? `
                        <button class="action-btn p-1.5 hover:bg-gray-100 dark:hover:bg-gray-700 rounded text-gray-500 hover:text-red-600 dark:text-gray-400 transition-colors" 
                                onclick="adminUsers.blockUser(${user.id})" 
                                title="Zablokuj"
                                ${!canBlock ? 'disabled style="opacity:0.5; cursor:not-allowed;"' : ''}>
                            <span class="material-symbols-outlined !text-xl">block</span>
                        </button>
                        ` : `
                        <button class="action-btn p-1.5 hover:bg-gray-100 dark:hover:bg-gray-700 rounded text-gray-500 hover:text-green-600 dark:text-gray-400 transition-colors" 
                                onclick="adminUsers.unblockUser(${user.id})" 
                                title="Odblokuj">
                            <span class="material-symbols-outlined !text-xl">lock_open</span>
                        </button>
                        `}
                        
                        <button class="action-btn p-1.5 hover:bg-gray-100 dark:hover:bg-gray-700 rounded text-gray-500 hover:text-red-600 dark:text-gray-400 transition-colors" 
                                onclick="adminUsers.deleteUser(${user.id})" 
                                title="Usuń" ${isCurrentUser ? 'disabled style="opacity:0.5; cursor:not-allowed;"' : ''}>
                            <span class="material-symbols-outlined !text-xl">delete</span>
                        </button>
                    </div>
                </td>
            </tr>
        `}).join('');
    }

    // --- Logika Modala Rangi ---

    openRankModal(userId, username, currentRankId) {
        this.selectedUserId = userId;
        const modal = document.getElementById('rank-modal');
        const usernameSpan = document.getElementById('modal-username');
        const select = document.getElementById('modal-rank-select');

        if (!modal || !select) return;

        if (usernameSpan) usernameSpan.textContent = username;

        // Wypełnij select opcjami
        select.innerHTML = this.availableRanks.map(rank => `
            <option value="${rank.id}" ${rank.id === currentRankId ? 'selected' : ''}>
                ${rank.name} (min. ${rank.minMessages} wiad.)
            </option>
        `).join('');

        modal.classList.remove('hidden');
        modal.classList.add('flex'); 
    }

    closeRankModal() {
        const modal = document.getElementById('rank-modal');
        if (modal) {
            modal.classList.add('hidden');
            modal.classList.remove('flex');
        }
        this.selectedUserId = null;
    }

    async confirmRankChange() {
        if (!this.selectedUserId) return;
        
        const select = document.getElementById('modal-rank-select');
        const newRankId = parseInt(select.value);

        try {
            const response = await fetch(`/api/users/${this.selectedUserId}/rank`, {
                method: 'PUT',
                headers: { 
                    'Content-Type': 'application/json' 
                },
                credentials: 'include',
                body: JSON.stringify(newRankId)
            });

            if (response.ok) {
                const data = await response.json();
                if(window.adminPanel) window.adminPanel.showSuccess(data.message || 'Ranga zmieniona pomyślnie');
                else alert('Ranga zmieniona pomyślnie');
                
                this.closeRankModal();
                this.loadUsers(); 
            } else {
                const error = await response.text();
                try {
                    const jsonError = JSON.parse(error);
                    if(window.adminPanel) window.adminPanel.showError(jsonError.error || 'Błąd zmiany rangi');
                    else alert(jsonError.error || 'Błąd zmiany rangi');
                } catch {
                    alert(`Błąd: ${error}`);
                }
            }
        } catch (error) {
            console.error(error);
            alert('Wystąpił błąd połączenia');
        }
    }

    // --- Metody pomocnicze ---

    updatePagination(total) {
        this.totalUsers = total;
        const totalPages = Math.ceil(total / this.pageSize) || 1;
        
        const resultsStart = document.getElementById('results-start');
        const resultsEnd = document.getElementById('results-end');
        const resultsTotal = document.getElementById('results-total');

        if (resultsStart) resultsStart.textContent = total === 0 ? 0 : ((this.currentPage - 1) * this.pageSize) + 1;
        if (resultsEnd) resultsEnd.textContent = Math.min(this.currentPage * this.pageSize, total);
        if (resultsTotal) resultsTotal.textContent = total;

        const prevBtn = document.getElementById('prev-page');
        const nextBtn = document.getElementById('next-page');

        if (prevBtn) prevBtn.disabled = this.currentPage <= 1;
        if (nextBtn) nextBtn.disabled = this.currentPage >= totalPages;

        this.generatePageNumbers(totalPages);
    }

    generatePageNumbers(totalPages) {
        const container = document.getElementById('pagination-numbers');
        if (!container) return;

        const buttons = [];
        const maxButtons = 5;
        let startPage = Math.max(1, this.currentPage - Math.floor(maxButtons / 2));
        let endPage = Math.min(totalPages, startPage + maxButtons - 1);

        if (endPage - startPage + 1 < maxButtons) {
            startPage = Math.max(1, endPage - maxButtons + 1);
        }

        for (let i = startPage; i <= endPage; i++) {
            buttons.push(`
                <button class="w-8 h-8 flex items-center justify-center rounded-lg text-sm font-medium transition-colors ${
                    i === this.currentPage 
                    ? 'bg-primary text-white' 
                    : 'text-gray-600 dark:text-gray-400 hover:bg-gray-100 dark:hover:bg-[#232f48]'
                }" 
                onclick="adminUsers.goToPage(${i})" 
                ${i === this.currentPage ? 'disabled' : ''}>
                    ${i}
                </button>
            `);
        }

        container.innerHTML = buttons.join('');
    }

    goToPage(page) {
        this.currentPage = page;
        this.loadUsers();
    }

    getRoleBadgeClass(role) {
        const classes = {
            'admin': 'bg-red-100 text-red-800 dark:bg-red-900/30 dark:text-red-400',
            'moderator': 'bg-yellow-100 text-yellow-800 dark:bg-yellow-900/30 dark:text-yellow-400',
            'user': 'bg-gray-100 text-gray-800 dark:bg-gray-900/30 dark:text-gray-400'
        };
        return classes[String(role).toLowerCase()] || classes['user'];
    }

    getRoleLabel(role) {
        const labels = {
            'admin': 'Administrator',
            'moderator': 'Moderator',
            'user': 'Użytkownik'
        };
        return labels[String(role).toLowerCase()] || 'Użytkownik';
    }

    getStatusBadgeClass(status) {
        const classes = {
            'active': 'bg-green-100 text-green-800 dark:bg-green-900/30 dark:text-green-400',
            'blocked': 'bg-red-100 text-red-800 dark:bg-red-900/30 dark:text-red-400',
            'pending': 'bg-yellow-100 text-yellow-800 dark:bg-yellow-900/30 dark:text-yellow-400'
        };
        return classes[status] || classes['active'];
    }

    formatDate(dateString) {
        if (!dateString) return 'Nigdy';
        const date = new Date(dateString);
        return date.toLocaleDateString('pl-PL', {
            year: 'numeric',
            month: '2-digit',
            day: '2-digit',
            hour: '2-digit',
            minute: '2-digit'
        });
    }

    async editUser(userId) {
        try {
            const userIdNum = parseInt(userId, 10);
            let user = this.allUsers.find(u => u.id === userIdNum);

            if (!user) return; 

            const modal = document.createElement('div');
            modal.id = 'edit-user-modal';
            modal.className = 'fixed inset-0 bg-black/50 z-50 flex items-center justify-center p-4';
            modal.innerHTML = `
                <div class="bg-white dark:bg-[#192231] rounded-lg shadow-lg max-w-sm w-full p-6 border border-gray-200 dark:border-gray-700">
                    <div class="flex items-center justify-between mb-4">
                        <h2 class="text-xl font-bold text-gray-900 dark:text-white">Edytuj rolę: ${user.username}</h2>
                        <button class="text-gray-500 hover:text-gray-700 dark:text-gray-400 dark:hover:text-gray-200" onclick="document.getElementById('edit-user-modal')?.remove()">
                            <span class="material-symbols-outlined">close</span>
                        </button>
                    </div>
                    <form id="edit-user-form" class="space-y-4">
                        <div>
                            <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">Rola</label>
                            <select id="user-role" class="w-full px-3 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-[#232f48] text-gray-900 dark:text-white">
                                <option value="User" ${String(user.role).toLowerCase() === 'user' ? 'selected' : ''}>Użytkownik</option>
                                <option value="Moderator" ${String(user.role).toLowerCase() === 'moderator' ? 'selected' : ''}>Moderator</option>
                                <option value="Admin" ${String(user.role).toLowerCase() === 'admin' ? 'selected' : ''}>Administrator</option>
                            </select>
                        </div>
                        <div class="flex gap-2 justify-end">
                            <button type="button" class="px-4 py-2 border rounded-lg dark:text-white" onclick="document.getElementById('edit-user-modal')?.remove()">Anuluj</button>
                            <button type="submit" class="px-4 py-2 bg-primary text-white rounded-lg">Zapisz</button>
                        </div>
                    </form>
                </div>
            `;
            document.body.appendChild(modal);

            document.getElementById('edit-user-form').addEventListener('submit', async (e) => {
                e.preventDefault();
                const newRole = document.getElementById('user-role').value;
                
                try {
                    const response = await fetch(`/api/users/${userId}`, {
                        method: 'PUT',
                        headers: { 'Content-Type': 'application/json' },
                        credentials: 'include',
                        body: JSON.stringify({ ...user, role: newRole })
                    });

                    if (response.ok) {
                        if(window.adminPanel) window.adminPanel.showSuccess('Rola zmieniona');
                        modal.remove();
                        this.loadUsers();
                    } else {
                        alert('Błąd zmiany roli');
                    }
                } catch (err) {
                    console.error(err);
                    alert('Błąd połączenia');
                }
            });
        } catch (error) {
            console.error(error);
        }
    }

    async deleteUser(userId) {
        if (confirm('Czy na pewno trwale usunąć tego użytkownika?')) {
            try {
                const response = await fetch(`/api/users/${userId}`, { 
                    method: 'DELETE',
                    credentials: 'include'
                });
                
                if (response.ok) {
                    if(window.adminPanel) window.adminPanel.showSuccess('Użytkownik usunięty');
                    this.loadUsers();
                } else {
                    try {
                       const json = await response.json();
                       alert(json.error || 'Błąd usuwania');
                    } catch {
                       alert('Błąd usuwania (Status: ' + response.status + ')');
                    }
                }
            } catch (e) {
                alert('Błąd połączenia');
            }
        }
    }
    
    async blockUser(userId) {
        if (!confirm('Czy na pewno zablokować tego użytkownika?')) return;
        
        const user = this.allUsers.find(u => u.id === userId);
        if (!user) return;

        try {
            const response = await fetch(`/api/users/${userId}`, {
                method: 'PUT',
                headers: { 'Content-Type': 'application/json' },
                credentials: 'include',
                body: JSON.stringify({ ...user, isBanned: true }) 
            });

            if (response.ok) {
                if(window.adminPanel) window.adminPanel.showSuccess('Użytkownik zablokowany');
                this.loadUsers();
            } else {
                alert('Błąd blokowania');
            }
        } catch (e) {
            console.error(e);
            alert('Błąd połączenia');
        }
    }

    async unblockUser(userId) {
        if (!confirm('Czy odblokować użytkownika?')) return;

        const user = this.allUsers.find(u => u.id === userId);
        if (!user) return;

        try {
            const response = await fetch(`/api/users/${userId}`, {
                method: 'PUT',
                headers: { 'Content-Type': 'application/json' },
                credentials: 'include',
                body: JSON.stringify({ ...user, isBanned: false })
            });

            if (response.ok) {
                if(window.adminPanel) window.adminPanel.showSuccess('Użytkownik odblokowany');
                this.loadUsers();
            } else {
                alert('Błąd odblokowania');
            }
        } catch (e) {
            console.error(e);
            alert('Błąd połączenia');
        }
    }
}

document.addEventListener('DOMContentLoaded', () => {
    window.adminUsers = new AdminUsers();
});

window.closeRankModal = () => window.adminUsers.closeRankModal();
window.confirmRankChange = () => window.adminUsers.confirmRankChange();