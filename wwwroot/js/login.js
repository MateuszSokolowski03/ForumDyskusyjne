// Login Page JavaScript — brak top-level await, wszystko w async functionach

document.addEventListener('DOMContentLoaded', initializeLoginPage);

function initializeLoginPage() {
    setupLoginForm();
    checkBannedStatus();
    console.log('🔐 Strona logowania zainicjalizowana');
}

function checkBannedStatus() {
    const params = new URLSearchParams(window.location.search);
    if (params.get('banned') === '1') {
        showErrorMessage('Twoje konto zostało zablokowane. Skontaktuj się z administratorem.');
        window.history.replaceState({}, document.title, '/login.html');
    }
}

function setupLoginForm() {
    const loginForm = document.getElementById('loginForm');
    if (!loginForm) return;
    loginForm.addEventListener('submit', handleLoginSubmit);
}

async function handleLoginSubmit(e) {
    e.preventDefault();
    const form = e.target;
    const formData = new FormData(form);

    const username = formData.get('username') || document.getElementById('username')?.value || '';
    const password = formData.get('password') || document.getElementById('password')?.value || '';
    const rememberMe = (formData.get('rememberMe') === 'on') || document.getElementById('remember-me')?.checked === true;

    const loginData = { username, password, rememberMe };

    removeOldMessages();
    showLoadingState(form);

    try {
        console.log('🔍 Próba logowania:', loginData.username);

        const response = await fetch('/api/auth/login', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            credentials: 'include',
            body: JSON.stringify(loginData)
        });

        if (!response.ok) {
            let errorMsg = 'Nieprawidłowe dane logowania';
            try {
                const err = await response.json();
                errorMsg = err?.message || err?.error || JSON.stringify(err);
            } catch {
                const txt = await response.text();
                if (txt) errorMsg = txt;
            }
            showErrorMessage(errorMsg);
            return;
        }

        let result = null;
        try { result = await response.json(); } catch {}

        showSuccessMessage('Logowanie udane. Przekierowywanie...');

        // role z response -> cookie -> status endpoint
        let role = result?.user?.role || result?.role || '';
        if (!role) {
            const raw = document.cookie.split('; ').find(c => c.startsWith('user_session='));
            if (raw) {
                try {
                    const val = decodeURIComponent(raw.split('=')[1]);
                    const parts = val.split('|');
                    if (parts.length >= 3) role = parts[2];
                } catch {}
            }
        }
        if (!role) {
            try {
                const s = await fetch('/api/auth/status', { credentials: 'include' });
                if (s.ok) {
                    const st = await s.json();
                    role = st?.user?.role || st?.role || '';
                }
            } catch {}
        }

        const roleStr = (role || '').toString().toLowerCase();
        const redirectTo = roleStr.includes('admin') ? '/admin' : '/index.html';
        setTimeout(() => (window.location.href = redirectTo), 600);
    } catch (err) {
        console.error('❌ Błąd połączenia:', err);
        showErrorMessage('Błąd połączenia z serwerem. Spróbuj ponownie.');
    } finally {
        hideLoadingState(form);
    }
}

function showLoadingState(form) {
    if (!form) return;
    form.classList.add('login-form-loading');
    const submitButton = form.querySelector('button[type="submit"]');
    if (submitButton) {
        submitButton.disabled = true;
        submitButton.dataset.origHtml = submitButton.innerHTML;
        submitButton.innerHTML = '<span class="truncate">Logowanie...</span>';
    }
}

function hideLoadingState(form) {
    if (!form) return;
    form.classList.remove('login-form-loading');
    const submitButton = form.querySelector('button[type="submit"]');
    if (submitButton) {
        submitButton.disabled = false;
        submitButton.innerHTML = submitButton.dataset.origHtml || '<span class="truncate">Zaloguj się</span>';
        delete submitButton.dataset.origHtml;
    }
}

function showErrorMessage(message) {
    const form = document.getElementById('loginForm') || document.body;
    const errorDiv = document.createElement('div');
    errorDiv.className = 'login-error bg-red-100 dark:bg-red-900/20 text-red-800 dark:text-red-300 p-3 rounded mb-3';
    errorDiv.textContent = message;
    form.prepend(errorDiv);
}

function showSuccessMessage(message) {
    const form = document.getElementById('loginForm') || document.body;
    const successDiv = document.createElement('div');
    successDiv.className = 'login-success bg-green-100 dark:bg-green-900/20 text-green-800 dark:text-green-300 p-3 rounded mb-3';
    successDiv.textContent = message;
    form.prepend(successDiv);
}

function removeOldMessages() {
    document.querySelectorAll('.login-error, .login-success').forEach(el => el.remove());
}