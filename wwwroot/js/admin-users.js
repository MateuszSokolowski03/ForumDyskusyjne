// Admin Users Management

class AdminUsers {
    constructor() {
        this.currentPage = 1;
        this.pageSize = 10;
        this.totalUsers = 0;
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

        tableBody.innerHTML = users.map(user => `
            <tr class="hover:bg-gray-50 dark:hover:bg-[#192231]">
                <td class="user-info">
                    <div class="flex items-center gap-3">
                        <div class="user-avatar w-10 h-10 bg-gray-200 dark:bg-gray-700 rounded-full flex items-center justify-center">
                            ${user.avatar_url ? 
                                `<img src="${user.avatar_url}" alt="${user.username}" class="w-full h-full rounded-full object-cover">` :
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
                        <button class="action-btn ${user.status === 'blocked' ? 'unblock-btn' : 'block-btn'}" 
                                onclick="adminUsers.${user.status === 'blocked' ? 'unblockUser' : 'blockUser'}(${user.id})" 
                                title="${user.status === 'blocked' ? 'Odblokuj' : 'Zablokuj'}">
                            <span class="material-symbols-outlined !text-base">${user.status === 'blocked' ? 'lock_open' : 'block'}</span>
                        </button>
                        <button class="action-btn delete-btn" onclick="adminUsers.deleteUser(${user.id})" title="Usuń">
                            <span class="material-symbols-outlined !text-base">delete</span>
                        </button>
                    </div>
                </td>
            </tr>
        `).join('');
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
        return classes[role] || classes['user'];
    }

    getRoleLabel(role) {
        const labels = {
            'admin': 'Administrator',
            'moderator': 'Moderator',
            'user': 'Użytkownik'
        };
        return labels[role] || 'Użytkownik';
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
        // Implementation for editing user
        window.adminPanel.showSuccess('Funkcja edycji użytkownika w przygotowaniu');
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
