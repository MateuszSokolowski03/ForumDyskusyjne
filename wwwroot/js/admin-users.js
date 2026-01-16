// Admin Users Management

class AdminUsers {
    constructor() {
        this.currentPage = 1;
        this.pageSize = 10;
        this.totalUsers = 0;
        this.allUsers = []; // Cache wszystkich użytkowników
        this.filters = {
            search: '',
            role: '',
            status: ''
        };
        this.init();
    }

    init() {
        this.setupEventListeners();
        this.loadUsers();
    }

    setupEventListeners() {
        // Search input
        const searchInput = document.getElementById('search-users');
        if (searchInput) {
            searchInput.addEventListener('input', (e) => {
                this.filters.search = e.target.value;
                this.debounceSearch();
            });
        }

        // Filter selects
        const roleFilter = document.getElementById('filter-role');
        const statusFilter = document.getElementById('filter-status');

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

        // Filter button
        const filterBtn = document.querySelector('.filter-btn');
        if (filterBtn) {
            filterBtn.addEventListener('click', () => {
                this.loadUsers();
            });
        }

        // Add user button
        const addUserBtn = document.querySelector('.add-user-btn');
        if (addUserBtn) {
            addUserBtn.addEventListener('click', () => {
                this.showAddUserModal();
            });
        }

        // Pagination
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

    async loadUsers() {
        const tableBody = document.getElementById('users-table-body');
        if (!tableBody) return;

        // Show loading
        tableBody.innerHTML = `
            <tr>
                <td colspan="7" class="text-center py-8 text-gray-500 dark:text-gray-400">
                    <div class="loading-state">
                        <div class="spinner mx-auto mb-2"></div>
                        <p>Ładowanie użytkowników...</p>
                    </div>
                </td>
            </tr>
        `;

        try {
            const params = new URLSearchParams({
                page: this.currentPage,
                pageSize: this.pageSize,
                search: this.filters.search,
                role: this.filters.role,
                status: this.filters.status
            });

            const response = await fetch(`/api/admin/users?${params}`);
            
            if (response.ok) {
                const data = await response.json();
                this.renderUsers(data.users);
                this.updatePagination(data.total);
            } else {
                throw new Error('Błąd ładowania użytkowników');
            }
        } catch (error) {
            console.error('Error loading users:', error);
            tableBody.innerHTML = `
                <tr>
                    <td colspan="7" class="text-center py-8 text-red-500">
                        <p>Błąd ładowania użytkowników: ${error.message}</p>
                        <button class="btn-primary mt-2" onclick="adminUsers.loadUsers()">Spróbuj ponownie</button>
                    </td>
                </tr>
            `;
        }
    }

    renderUsers(users) {
        const tableBody = document.getElementById('users-table-body');
        if (!tableBody) return;

        // Cache użytkowników
        this.allUsers = users;

        if (users.length === 0) {
            tableBody.innerHTML = `
                <tr>
                    <td colspan="7" class="text-center py-8 text-gray-500 dark:text-gray-400">
                        <p>Nie znaleziono użytkowników</p>
                    </td>
                </tr>
            `;
            return;
        }

        const currentUser = window.auth.getUser();

        tableBody.innerHTML = users.map(user => {
            const isCurrentUser = currentUser && user.id === currentUser.id;
            const isAdmin = user.role.toLowerCase() === 'admin';
            const canBlock = !isCurrentUser && !isAdmin;

            return `
            <tr class="hover:bg-gray-50 dark:hover:bg-[#192231]">
                <td class="user-info">
                    <div class="flex items-center gap-3">
                        <div class="user-avatar w-10 h-10 bg-gray-200 dark:bg-gray-700 rounded-full flex items-center justify-center">
                            ${user.avatarUrl ? 
                                `<img src="${user.avatarUrl}" alt="${user.username}" class="w-full h-full rounded-full object-cover">` :
                                `<span class="material-symbols-outlined text-gray-500 dark:text-gray-400">person</span>`
                            }
                        </div>
                        <div>
                            <p class="font-medium text-gray-900 dark:text-white">${user.username}</p>
                            <p class="text-sm text-gray-500 dark:text-gray-400">ID: ${user.id}</p>
                        </div>
                    </div>
                </td>
                <td class="user-email text-gray-600 dark:text-gray-300">${user.email || 'Brak email'}</td>
                <td class="user-role">
                    <span class="role-badge inline-flex px-2 py-1 text-xs font-semibold rounded-full ${this.getRoleBadgeClass(user.role)}">
                        ${this.getRoleLabel(user.role)}
                    </span>
                </td>
                <td class="user-status">
                    <span class="status-badge inline-flex px-2 py-1 text-xs font-semibold rounded-full ${this.getStatusBadgeClass(user.status || 'active')}">
                        ${this.getStatusLabel(user.status || 'active')}
                    </span>
                </td>
                <td class="user-created text-gray-600 dark:text-gray-300">${this.formatDate(user.created_at)}</td>
                <td class="user-activity text-gray-600 dark:text-gray-300">${this.formatDate(user.last_activity_at)}</td>
                <td class="user-actions">
                    <div class="flex justify-center gap-2">
                        <button class="action-btn edit-btn" onclick="adminUsers.editUser(${user.id})" title="Edytuj">
                            <span class="material-symbols-outlined !text-base">edit</span>
                        </button>
                        ${user.status === 'pending' ? `
                        <button class="action-btn verify-btn" onclick="adminUsers.verifyUserEmail(${user.id})" title="Potwierdź email">
                            <span class="material-symbols-outlined !text-base">check_circle</span>
                        </button>
                        ` : `
                        <button class="action-btn ${user.status === 'blocked' ? 'unblock-btn' : 'block-btn'}" 
                                onclick="adminUsers.${user.status === 'blocked' ? 'unblockUser' : 'blockUser'}(${user.id})" 
                                title="${user.status === 'blocked' ? 'Odblokuj' : 'Zablokuj'}"
                                ${!canBlock ? 'disabled' : ''}>
                            <span class="material-symbols-outlined !text-base">${user.status === 'blocked' ? 'lock_open' : 'block'}</span>
                        </button>
                        `}
                        <button class="action-btn delete-btn" onclick="adminUsers.deleteUser(${user.id})" title="Usuń" ${isCurrentUser ? 'disabled' : ''}>
                            <span class="material-symbols-outlined !text-base">delete</span>
                        </button>
                    </div>
                </td>
            </tr>
        `}).join('');
    }

    updatePagination(total) {
        this.totalUsers = total;
        const totalPages = Math.ceil(total / this.pageSize);
        
        // Update info
        const resultsStart = document.getElementById('results-start');
        const resultsEnd = document.getElementById('results-end');
        const resultsTotal = document.getElementById('results-total');

        if (resultsStart) resultsStart.textContent = ((this.currentPage - 1) * this.pageSize) + 1;
        if (resultsEnd) resultsEnd.textContent = Math.min(this.currentPage * this.pageSize, total);
        if (resultsTotal) resultsTotal.textContent = total;

        // Update buttons
        const prevBtn = document.getElementById('prev-page');
        const nextBtn = document.getElementById('next-page');

        if (prevBtn) prevBtn.disabled = this.currentPage <= 1;
        if (nextBtn) nextBtn.disabled = this.currentPage >= totalPages;

        // Generate page numbers
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
                <button class="pagination-number ${i === this.currentPage ? 'active' : ''}" 
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
        return classes[role?.toLowerCase()] || classes['user'];
    }

    getRoleLabel(role) {
        const labels = {
            'admin': 'Administrator',
            'moderator': 'Moderator',
            'user': 'Użytkownik'
        };
        return labels[role?.toLowerCase()] || 'Użytkownik';
    }

    getStatusBadgeClass(status) {
        const classes = {
            'active': 'bg-green-100 text-green-800 dark:bg-green-900/30 dark:text-green-400',
            'blocked': 'bg-red-100 text-red-800 dark:bg-red-900/30 dark:text-red-400',
            'pending': 'bg-yellow-100 text-yellow-800 dark:bg-yellow-900/30 dark:text-yellow-400'
        };
        return classes[status] || classes['active'];
    }

    getStatusLabel(status) {
        const labels = {
            'active': 'Aktywny',
            'blocked': 'Zablokowany',
            'pending': 'Oczekujący'
        };
        return labels[status] || 'Aktywny';
    }

    formatDate(dateString) {
        if (!dateString) return 'Nigdy';
        const date = new Date(dateString);
        return date.toLocaleDateString('pl-PL', {
            year: 'numeric',
            month: 'short',
            day: 'numeric',
            hour: '2-digit',
            minute: '2-digit'
        });
    }

    showAddUserModal() {
        const modal = window.adminPanel.showModal('Dodaj nowego użytkownika', `
            <form id="add-user-form" class="admin-form">
                <div class="form-group">
                    <label class="form-label">Nazwa użytkownika</label>
                    <input type="text" name="username" class="form-input" required>
                </div>
                <div class="form-group">
                    <label class="form-label">Email</label>
                    <input type="email" name="email" class="form-input" required>
                </div>
                <div class="form-group">
                    <label class="form-label">Hasło</label>
                    <input type="password" name="password" class="form-input" required>
                </div>
                <div class="form-group">
                    <label class="form-label">Rola</label>
                    <select name="role" class="form-input">
                        <option value="user">Użytkownik</option>
                        <option value="moderator">Moderator</option>
                        <option value="admin">Administrator</option>
                    </select>
                </div>
            </form>
        `, [
            { text: 'Anuluj', type: 'secondary' },
            { text: 'Dodaj użytkownika', type: 'primary', action: 'adminUsers.submitAddUser()' }
        ]);
    }

    async submitAddUser() {
        const form = document.getElementById('add-user-form');
        const formData = new FormData(form);
        const userData = Object.fromEntries(formData);

        try {
            const response = await fetch('/api/admin/users', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(userData)
            });

            if (response.ok) {
                window.adminPanel.showSuccess('Użytkownik został dodany pomyślnie');
                document.querySelector('.modal-overlay').remove();
                this.loadUsers();
            } else {
                const error = await response.text();
                window.adminPanel.showError(`Błąd: ${error}`);
            }
        } catch (error) {
            window.adminPanel.showError('Wystąpił błąd podczas dodawania użytkownika');
        }
    }

    async editUser(userId) {
        try {
            // Pobierz dane użytkownika z cache'owanej listy
            const userIdNum = parseInt(userId, 10);
            let user = this.allUsers.find(u => u.id === userIdNum);

            // Jeśli nie ma w cache, pobierz wszystkich użytkowników bez paginacji
            if (!user) {
                const response = await fetch(`/api/admin/users?pageSize=10000`);
                if (!response.ok) {
                    window.adminPanel.showError('Błąd ładowania danych użytkownika');
                    return;
                }
                const data = await response.json();
                user = data.users.find(u => u.id === userIdNum);
            }

            if (!user) {
                window.adminPanel.showError('Użytkownik nie znaleziony');
                return;
            }

            // Pokaż modal edycji
            const modal = document.createElement('div');
            modal.id = 'edit-user-modal';
            modal.className = 'fixed inset-0 bg-black/50 z-50 flex items-center justify-center p-4';
            modal.innerHTML = `
                <div class="bg-white dark:bg-[#192231] rounded-lg shadow-lg max-w-md w-full p-6">
                    <div class="flex items-center justify-between mb-4">
                        <h2 class="text-xl font-bold text-gray-900 dark:text-white">Edytuj użytkownika: ${user.username}</h2>
                        <button class="text-gray-500 hover:text-gray-700 dark:text-gray-400 dark:hover:text-gray-200" onclick="document.getElementById('edit-user-modal')?.remove()">
                            <span class="material-symbols-outlined">close</span>
                        </button>
                    </div>

                    <form id="edit-user-form" class="space-y-4">
                        <div>
                            <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">Rola</label>
                            <select id="user-role" class="w-full px-3 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-[#232f48] text-gray-900 dark:text-white focus:outline-none focus:border-primary focus:ring-1 focus:ring-primary">
                                <option value="user" ${user.role === 'user' ? 'selected' : ''}>Użytkownik</option>
                                <option value="moderator" ${user.role === 'moderator' ? 'selected' : ''}>Moderator</option>
                                <option value="admin" ${user.role === 'admin' ? 'selected' : ''}>Administrator</option>
                            </select>
                        </div>

                        <div class="bg-blue-50 dark:bg-blue-900/30 border border-blue-200 dark:border-blue-800 rounded-lg p-3">
                            <p class="text-sm text-blue-800 dark:text-blue-300">
                                <strong>Aktualna rola:</strong> ${this.getRoleLabel(user.role)}
                            </p>
                        </div>

                        <div class="flex gap-2 justify-end">
                            <button type="button" class="px-4 py-2 text-gray-700 dark:text-gray-300 border border-gray-300 dark:border-gray-600 rounded-lg hover:bg-gray-100 dark:hover:bg-[#232f48] transition" onclick="document.getElementById('edit-user-modal')?.remove()">
                                Anuluj
                            </button>
                            <button type="submit" class="px-4 py-2 bg-primary hover:bg-primary/90 text-white rounded-lg transition">
                                Zapisz zmiany
                            </button>
                        </div>
                    </form>
                </div>
            `;

            document.body.appendChild(modal);

            // Obsłuż klik poza modalem
            modal.addEventListener('click', (e) => {
                if (e.target === modal) modal.remove();
            });

            // Obsłuż wysłanie formularza
            document.getElementById('edit-user-form').addEventListener('submit', async (e) => {
                e.preventDefault();
                const newRole = document.getElementById('user-role').value;

                if (newRole === user.role) {
                    window.adminPanel.showInfo('Żaden zmian nie został dokonany');
                    return;
                }

                try {
                    const response = await fetch(`/api/admin/users/${userId}/role`, {
                        method: 'PUT',
                        headers: { 'Content-Type': 'application/json' },
                        body: JSON.stringify({ 
                            Role: newRole.charAt(0).toUpperCase() + newRole.slice(1) // Kapitalizuj pierwszą literę
                        })
                    });

                    if (response.ok) {
                        window.adminPanel.showSuccess(`Rola użytkownika zmieniona na: ${this.getRoleLabel(newRole)}`);
                        modal.remove();
                        this.loadUsers();
                    } else {
                        const error = await response.text();
                        window.adminPanel.showError(`Błąd: ${error}`);
                    }
                } catch (error) {
                    window.adminPanel.showError('Błąd podczas zmiany roli użytkownika');
                    console.error('Error:', error);
                }
            });

        } catch (error) {
            window.adminPanel.showError('Błąd podczas otwierania edytora');
            console.error('Error:', error);
        }
    }

    async verifyUserEmail(userId) {
        if (confirm('Czy na pewno chcesz potwierdzić email tego użytkownika?')) {
            try {
                const response = await fetch(`/api/admin/users/${userId}/verify-email`, {
                    method: 'POST'
                });

                if (response.ok) {
                    window.adminPanel.showSuccess('Email użytkownika został potwierdzony');
                    this.loadUsers();
                } else {
                    window.adminPanel.showError('Błąd podczas potwierdzania emaila');
                }
            } catch (error) {
                window.adminPanel.showError('Błąd podczas potwierdzania emaila');
            }
        }
    }

    async blockUser(userId) {
        if (confirm('Czy na pewno chcesz zablokować tego użytkownika?')) {
            try {
                const response = await fetch(`/api/admin/users/${userId}/block`, {
                    method: 'POST'
                });

                if (response.ok) {
                    window.adminPanel.showSuccess('Użytkownik został zablokowany');
                    this.loadUsers();
                } else {
                    window.adminPanel.showError('Błąd podczas blokowania użytkownika');
                }
            } catch (error) {
                window.adminPanel.showError('Błąd podczas blokowania użytkownika');
            }
        }
    }

    async unblockUser(userId) {
        if (confirm('Czy na pewno chcesz odblokować tego użytkownika?')) {
            try {
                const response = await fetch(`/api/admin/users/${userId}/unblock`, {
                    method: 'POST'
                });

                if (response.ok) {
                    window.adminPanel.showSuccess('Użytkownik został odblokowany');
                    this.loadUsers();
                } else {
                    window.adminPanel.showError('Błąd podczas odblokowywania użytkownika');
                }
            } catch (error) {
                window.adminPanel.showError('Błąd podczas odblokowywania użytkownika');
            }
        }
    }

    async deleteUser(userId) {
        if (confirm('Czy na pewno chcesz usunąć tego użytkownika? Ta operacja jest nieodwracalna!')) {
            try {
                const response = await fetch(`/api/admin/users/${userId}`, {
                    method: 'DELETE'
                });

                if (response.ok) {
                    window.adminPanel.showSuccess('Użytkownik został usunięty');
                    this.loadUsers();
                } else {
                    window.adminPanel.showError('Błąd podczas usuwania użytkownika');
                }
            } catch (error) {
                window.adminPanel.showError('Błąd podczas usuwania użytkownika');
            }
        }
    }
}

// Initialize
document.addEventListener('DOMContentLoaded', () => {
    window.adminUsers = new AdminUsers();
});
