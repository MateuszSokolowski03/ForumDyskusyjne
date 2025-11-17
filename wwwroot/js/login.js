// Login Page JavaScript

document.addEventListener('DOMContentLoaded', function() {
    initializeLoginPage();
});

function initializeLoginPage() {
    setupLoginForm();
    console.log('🔐 Strona logowania zainicjalizowana');
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
    const loginData = {
        username: formData.get('username'),
        password: formData.get('password'),
        rememberMe: formData.get('rememberMe') === 'on'
    };
    
    console.log('🔍 Próba logowania:', loginData.username);
    
    // Pokaż loading
    showLoadingState(form);
    
    try {
        const response = await fetch('/api/auth/login', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(loginData)
        });
        
        if (response.ok) {
            const result = await response.json();
            console.log('✅ Logowanie udane:', result);
            
            showSuccessMessage('Logowanie udane! Przekierowywanie...');
            
            // Przekierowanie po krótkim opóźnieniu - serwer już ustawił cookies
            setTimeout(() => {
                window.location.href = '/index.html';
            }, 1500);
        } else {
            const error = await response.json();
            console.log('❌ Błąd logowania:', error.message);
            showErrorMessage(error.message || 'Nieprawidłowe dane logowania');
        }
    } catch (error) {
        console.error('❌ Błąd połączenia:', error);
        showErrorMessage('Błąd połączenia z serwerem. Spróbuj ponownie.');
    } finally {
        hideLoadingState(form);
    }
}

function showLoadingState(form) {
    form.classList.add('login-form-loading');
    const submitButton = form.querySelector('button[type="submit"]');
    if (submitButton) {
        submitButton.disabled = true;
        submitButton.innerHTML = '<span class="truncate">Logowanie...</span>';
    }
}

function hideLoadingState(form) {
    form.classList.remove('login-form-loading');
    const submitButton = form.querySelector('button[type="submit"]');
    if (submitButton) {
        submitButton.disabled = false;
        submitButton.innerHTML = '<span class="truncate">Zaloguj się</span>';
    }
}

function showErrorMessage(message) {
    removeOldMessages();
    const form = document.getElementById('loginForm');
    const errorDiv = document.createElement('div');
    errorDiv.className = 'login-error';
    errorDiv.textContent = message;
    form.appendChild(errorDiv);
}

function showSuccessMessage(message) {
    removeOldMessages();
    const form = document.getElementById('loginForm');
    const successDiv = document.createElement('div');
    successDiv.className = 'login-success';
    successDiv.textContent = message;
    form.appendChild(successDiv);
}

function removeOldMessages() {
    const oldErrors = document.querySelectorAll('.login-error, .login-success');
    oldErrors.forEach(el => el.remove());
}
