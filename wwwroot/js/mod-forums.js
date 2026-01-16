document.addEventListener('DOMContentLoaded', () => {
    loadModForums();
});

async function loadModForums() {
    const container = document.getElementById('mod-forums-list');
    if (!container) {
        console.error('Container #mod-forums-list not found');
        return;
    }

    container.innerHTML = '<p class="text-gray-500">Ładowanie forów...</p>';

    try {
        const response = await fetch('/api/mod/forums');
        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }
        const forums = await response.json();
        renderForums(forums);
    } catch (error) {
        console.error('Error loading moderated forums:', error);
        container.innerHTML = '<p class="text-red-500">Wystąpił błąd podczas ładowania forów.</p>';
    }
}

function renderForums(forums) {
    const container = document.getElementById('mod-forums-list');
    if (!container) return;

    if (!forums || forums.length === 0) {
        container.innerHTML = '<p class="text-gray-500">Nie jesteś moderatorem żadnego forum.</p>';
        return;
    }

    const forumsHtml = forums.map(forum => `
        <div class="category-card bg-white dark:bg-[#192231] rounded-lg border border-gray-200 dark:border-[#232f48] p-6 mb-6">
            <div class="flex items-center justify-between mb-4">
                <div class="flex-1">
                    <h3 class="text-lg font-bold text-gray-900 dark:text-white">${escapeHtml(forum.name)}</h3>
                    <p class="text-sm text-gray-500 dark:text-gray-400">${escapeHtml(forum.description || '')}</p>
                </div>
            </div>
            
            <div class="forums-list space-y-2" id="forums-${forum.id}">
                <div class="forum-item flex justify-between items-center p-3 bg-gray-50 dark:bg-[#101622] rounded border border-gray-200 dark:border-[#232f48]">
                    <div class="flex-1">
                        <h4 class="font-semibold text-gray-900 dark:text-white">Statystyki</h4>
                        <div class="mt-1 flex gap-4 text-xs text-gray-500">
                            <span>📊 ${forum.threadCount || 0} wątków</span>
                            <span>💬 ${forum.messageCount || 0} postów</span>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    `).join('');

    container.innerHTML = forumsHtml;
}

function escapeHtml(text) {
    if (text === null || typeof text === 'undefined') {
        return '';
    }
    return String(text).replace(/[&<>"']/g, function(match) {
        return {
            '&': '&amp;',
            '<': '&lt;',
            '>': '&gt;',
            '"': '&quot;',
            "'": '&#39;'
        }[match];
    });
}