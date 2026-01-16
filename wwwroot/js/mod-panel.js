// Skrypt do panelu moderatora
// Wymaga backendu, który zwraca fora, użytkowników, wątki tylko z forów przypisanych do moderatora

document.addEventListener('DOMContentLoaded', function() {
    loadModForums();
    loadModUsers();
    loadModCategories();
    loadModThreads();
});

// Załaduj fora moderatora
async function loadModForums() {
    const container = document.getElementById('mod-forums-list');
    if (!container) return;

    try {
        const res = await fetch('/api/mod/forums', { credentials: 'include' });
        if (!res.ok) throw new Error('Błąd API');
        const forums = await res.json();
        if (!forums.length) {
            container.innerHTML = '<p class="text-gray-500">Nie przypisano zadnego forum.</p>';
            return;
        }
        container.innerHTML = forums.map(f => `<div class="mb-2 p-3 rounded bg-primary/10 dark:bg-primary/20 text-primary dark:text-white">${escapeHtml(f.name || f.Name)}</div>`).join('');
    } catch (e) {
        container.innerHTML = '<p class="text-red-500">Błąd ładowania forów.</p>';
    }
}

// Załaduj użytkowników z forów moderatora
async function loadModUsers() {
    const container = document.getElementById('mod-users-list');
    if (!container) return;

    try {
        const res = await fetch('/api/mod/users', { credentials: 'include' });
        if (!res.ok) throw new Error('Błąd API');
        const users = await res.json();
        if (!users.length) {
            container.innerHTML = '<p class="text-gray-500">Brak użytkowników.</p>';
            return;
        }
        
        const tableRows = users.map(u => {
            let actionButton = '';
            if (u.role === 'Admin') {
                actionButton = `
                    <button class="action-btn" title="Nie można zablokować administratora" disabled 
                            style="background-color: #6b7280; color: white; cursor: not-allowed; display: inline-flex; align-items: center; justify-content: center; width: 2rem; height: 2rem; border-radius: 0.375rem;">
                        <span class="material-symbols-outlined !text-base">verified_user</span>
                    </button>`;
            } else if (u.isBlocked) {
                actionButton = `
                    <button class="action-btn unblock-btn" 
                            onclick="toggleBlockStatus(${u.id}, true)" 
                            title="Odblokuj"
                            style="background-color: #10b981; color: white; display: inline-flex; align-items: center; justify-content: center; width: 2rem; height: 2rem; border-radius: 0.375rem;">
                        <span class="material-symbols-outlined !text-base">lock_open</span>
                    </button>`;
            } else {
                actionButton = `
                    <button class="action-btn block-btn" 
                            onclick="toggleBlockStatus(${u.id}, false)" 
                            title="Zablokuj"
                            style="background-color: #ef4444; color: white; display: inline-flex; align-items: center; justify-content: center; width: 2rem; height: 2rem; border-radius: 0.375rem;">
                        <span class="material-symbols-outlined !text-base">block</span>
                    </button>`;
            }

            return `<tr>
                        <td>${escapeHtml(u.username)}</td>
                        <td>${escapeHtml(u.email)}</td>
                        <td>${u.isBlocked ? 'Zablokowany' : 'Aktywny'}</td>
                        <td>${actionButton}</td>
                    </tr>`;
        }).join('');

        container.innerHTML = `<table class="w-full"><thead><tr><th>Nazwa</th><th>Email</th><th>Status</th><th>Akcje</th></tr></thead><tbody>${tableRows}</tbody></table>`;
    } catch (e) {
        container.innerHTML = '<p class="text-red-500">Błąd ładowania użytkowników.</p>';
    }
}

// Przełącz status blokady użytkownika
async function toggleBlockStatus(userId, isCurrentlyBlocked) {
    const action = isCurrentlyBlocked ? 'odblokować' : 'zablokować';
    if (!confirm(`Czy na pewno chcesz ${action} tego użytkownika?`)) return;

    const endpoint = `/api/mod/users/${userId}/${isCurrentlyBlocked ? 'unblock' : 'block'}`;
    
    try {
        const res = await fetch(endpoint, { method: 'POST', credentials: 'include' });
        if (!res.ok) {
            const error = await res.json();
            throw new Error(error.title || `Błąd ${action}wania`);
        }
        alert(`Użytkownik został ${isCurrentlyBlocked ? 'odblokowany' : 'zablokowany'}`);
        loadModUsers();
    } catch (e) {
        alert(`Błąd: ${e.message}`);
    }
}

// Załaduj przypisane fora do edycji
async function loadModCategories() {
    const container = document.getElementById('mod-categories-list');
    if (!container) return;

    try {
        const res = await fetch('/api/mod/forums', { credentials: 'include' });
        if (!res.ok) throw new Error('Błąd API');
        const forums = await res.json();
        if (!forums.length) {
            container.innerHTML = '<p class="text-gray-500">Brak forów do edycji.</p>';
            return;
        }
        container.innerHTML = forums.map(f => `<div class="mb-2 p-3 rounded bg-blue-100 dark:bg-blue-900 text-blue-800 dark:text-blue-300 flex justify-between items-center">${escapeHtml(f.name)} <button onclick="editForum(${f.id})" class="px-2 py-1 rounded bg-primary text-white">Edytuj</button></div>`).join('');
    } catch (e) {
        container.innerHTML = '<p class="text-red-500">Błąd ładowania forów.</p>';
    }
}

function editForum(forumId) {
    window.location.href = `/mod/edit-forum.html?forumId=${forumId}`;
}

// Załaduj wątki i wiadomości z forów moderatora
async function loadModThreads() {
    const container = document.getElementById('mod-threads-list');
    if (!container) return;

    try {
        const res = await fetch('/api/mod/threads', { credentials: 'include' });
        if (!res.ok) throw new Error('Błąd API');
        const threads = await res.json();
        if (!threads.length) {
            container.innerHTML = '<p class="text-gray-500">Brak wątków.</p>';
            return;
        }
        container.innerHTML = `<table class="w-full"><thead><tr><th>Tytuł</th><th>Autor</th><th>Forum</th><th>Wiadomości</th><th>Widoki</th><th>Data</th><th>Akcje</th></tr></thead><tbody>` +
            threads.map(t => `<tr><td>${escapeHtml(t.title)}</td><td>${escapeHtml(t.authorName)}</td><td>${escapeHtml(t.forumName)}</td><td>${t.repliesCount}</td><td>${t.views}</td><td>${new Date(t.createdAt).toLocaleDateString('pl-PL')}</td><td><button onclick="viewThread(${t.id})" class="px-2 py-1 rounded bg-blue-100 text-blue-700">Wyświetl</button><button onclick="deleteThread(${t.id})" class="px-2 py-1 rounded bg-red-100 text-red-700">Usuń</button></td></tr>`).join('') +
            `</tbody></table>`;
    } catch (e) {
        container.innerHTML = '<p class="text-red-500">Błąd ładowania wątków.</p>';
    }
}

function viewThread(threadId) {
    window.location.href = `/thread.html?threadId=${threadId}`;
}

async function deleteThread(threadId) {
    if (!confirm('Czy na pewno chcesz usunąć ten wątek?')) return;
    try {
        const res = await fetch(`/api/mod/threads/${threadId}`, { method: 'DELETE', credentials: 'include' });
        if (!res.ok) throw new Error('Błąd usuwania');
        alert('Wątek usunięty');
        loadModThreads();
    } catch (e) {
        alert('Błąd usuwania wątku');
    }
}

function escapeHtml(text) {
    if (!text) return '';
    return String(text).replace(/[&<>"]|'/g, m => ({
        '&': '&amp;',
        '<': '&lt;',
        '>': '&gt;',
        '"': '&quot;',
        "'": '&#039;'
    })[m]);
}
