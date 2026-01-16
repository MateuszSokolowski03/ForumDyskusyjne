// Mod Users Management

class ModUsers {
    constructor() {
        this.currentPage = 1;
        this.pageSize = 10;
        this.totalUsers = 0;
        this.filters = {
            search: ''
        };
        this.searchTimeout = null;
        this.init();
    }

    init() {
        this.setupEventListeners();
        this.loadUsers();
    }

    setupEventListeners() {
        const searchInput = document.getElementById('search-users');
        if (searchInput) {
            searchInput.addEventListener('input', (e) => {
                this.filters.search = e.target.value;
                this.debounceSearch();
            });
        }

        const prevBtn = document.getElementById('prev-page');
        if (prevBtn) {
            prevBtn.addEventListener('click', () => {
                if (this.currentPage > 1) {
                    this.currentPage--;
                    this.loadUsers();
                }
            });
        }

        const nextBtn = document.getElementById('next-page');
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

        tableBody.innerHTML = `
            <tr>
                <td colspan="4" class="text-center py-8 text-gray-500 dark:text-gray-400">
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
                search: this.filters.search
            });

            const response = await fetch(`/api/mod/users?${params}`);
            
            if (response.ok) {
                const data = await response.json();
                // Check if data is the expected object or a fallback array
                const users = Array.isArray(data) ? data : data.users;
                const total = Array.isArray(data) ? data.length : data.total;
                
                if (!users) {
                     throw new Error('Odpowiedź serwera nie zawiera listy użytkowników.');
                }

                this.renderUsers(users);
                this.updatePagination(total);
            } else {
                throw new Error('Błąd ładowania użytkowników');
            }
        } catch (error) {
            console.error('Error loading users:', error);
            tableBody.innerHTML = `
                <tr>
                    <td colspan="4" class="text-center py-8 text-red-500">
                        <p>Błąd ładowania użytkowników: ${error.message}</p>
                        <button class="btn-primary mt-2" onclick="modUsers.loadUsers()">Spróbuj ponownie</button>
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
                    <td colspan="4" class="text-center py-8 text-gray-500 dark:text-gray-400">
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
                    <td class="p-4">
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
                    <td class="p-4 text-gray-600 dark:text-gray-300">${user.email || 'Brak email'}</td>
                    <td class="p-4">
                        <span class="status-badge inline-flex px-2 py-1 text-xs font-semibold rounded-full ${this.getStatusBadgeClass(user.status || 'active')}">
                            ${this.getStatusLabel(user.status || 'active')}
                        </span>
                    </td>
                    <td class="p-4 text-center">
                        <button 
                            onclick="modUsers.${user.status === 'blocked' ? 'unblockUser' : 'blockUser'}(${user.id})"
                            class="action-btn ${user.status === 'blocked' ? 'unblock-btn' : 'block-btn'}"
                            title="${user.status === 'blocked' ? 'Odblokuj' : 'Zablokuj'}"
                            ${!canBlock ? 'disabled' : ''}>
                            <span class="material-symbols-outlined !text-base">${user.status === 'blocked' ? 'lock_open' : 'block'}</span>
                        </button>
                    </td>
                </tr>
            `;
        }).join('');
    }

    updatePagination(total) {
        this.totalUsers = total;
        const totalPages = Math.ceil(total / this.pageSize);
        
        const paginationInfo = document.getElementById('pagination-info');
        if (paginationInfo) {
            paginationInfo.textContent = `Wyświetlono ${((this.currentPage - 1) * this.pageSize) + 1}-${Math.min(this.currentPage * this.pageSize, total)} z ${total} użytkowników`;
        }

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
                <button class="pagination-number ${i === this.currentPage ? 'active' : ''}" 
                        onclick="modUsers.goToPage(${i})" 
                        ${i === this.currentPage ? 'aria-current="page"' : ''}>
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

    async blockUser(userId) {
        if (confirm('Czy na pewno chcesz zablokować tego użytkownika?')) {
            try {
                const response = await fetch(`/api/mod/users/${userId}/block`, {
                    method: 'POST'
                });

                if (response.ok) {
                    // find a better way to show notifications
                    alert('Użytkownik został zablokowany');
                    this.loadUsers();
                } else {
                    const error = await response.text();
                    alert(`Błąd podczas blokowania użytkownika: ${error}`);
                }
            } catch (error) {
                alert('Błąd podczas blokowania użytkownika');
            }
        }
    }

    async unblockUser(userId) {
        if (confirm('Czy na pewno chcesz odblokować tego użytkownika?')) {
            try {
                const response = await fetch(`/api/mod/users/${userId}/unblock`, {
                    method: 'POST'
                });

                if (response.ok) {
                    alert('Użytkownik został odblokowany');
                    this.loadUsers();
                } else {
                    const error = await response.text();
                    alert(`Błąd podczas odblokowywania użytkownika: ${error}`);
                }
            } catch (error) {
                alert('Błąd podczas odblokowywania użytkownika');
            }
        }
    }
}

document.addEventListener('DOMContentLoaded', () => {
    window.modUsers = new ModUsers();
});