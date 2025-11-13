// Register Page JavaScript

document.addEventListener('DOMContentLoaded', function() {
    initializeRegisterPage();
});

function initializeRegisterPage() {
    setupRegisterForm();
    setupPasswordValidation();
    setupAvatarPreview();
    console.log('📝 Strona rejestracji zainicjalizowana');
}

function setupRegisterForm() {
    const registerForm = document.getElementById('registerForm');
    if (!registerForm) return;

    registerForm.addEventListener('submit', handleRegisterSubmit);
}

function setupPasswordValidation() {
    const passwordInput = document.getElementById('password');
    const confirmPasswordInput = document.getElementById('confirm-password');
    
    if (passwordInput) {
        passwordInput.addEventListener('input', validatePasswordStrength);
    }
    
    if (confirmPasswordInput) {
        confirmPasswordInput.addEventListener('input', validatePasswordMatch);
    }
}

function setupAvatarPreview() {
    const avatarInput = document.getElementById('avatar-upload');
    const avatarPreview = document.querySelector('.avatar-preview');
    
    if (avatarInput && avatarPreview) {
        avatarInput.addEventListener('change', function(e) {
            const file = e.target.files[0];
            if (file && file.type.startsWith('image/')) {
                const reader = new FileReader();
                reader.onload = function(e) {
                    avatarPreview.innerHTML = `<img src="${e.target.result}" alt="Avatar preview" class="h-full w-full object-cover rounded-full">`;
                };
                reader.readAsDataURL(file);
            }
        });
    }
}

async function handleRegisterSubmit(e) {
    e.preventDefault();
    
    const form = e.target;
    const formData = new FormData(form);
    
    // Walidacja po stronie klienta
    if (!validateForm(formData)) {
        return;
    }
    
    const registerData = {
        username: formData.get('username'),
        email: formData.get('email'),
        password: formData.get('password'),
        confirmPassword: formData.get('confirmPassword'),
        terms: formData.get('terms') === 'on'
    };
    
    console.log('📝 Próba rejestracji:', registerData.username);
    
    // Pokaż loading
    showLoadingState(form);
    
    try {
        const response = await fetch('/api/auth/register', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(registerData)
        });
        
        if (response.ok) {
            const result = await response.json();
            console.log('✅ Rejestracja udana');
            showSuccessMessage('Konto zostało utworzone! Przekierowywanie do logowania...');
            
            // Przekierowanie po krótkim opóźnieniu
            setTimeout(() => {
                window.location.href = '/login';
            }, 2000);
        } else {
            const error = await response.json();
            console.log('❌ Błąd rejestracji:', error.message);
            showErrorMessage(error.message || 'Błąd podczas rejestracji');
        }
    } catch (error) {
        console.error('❌ Błąd połączenia:', error);
        showErrorMessage('Błąd połączenia z serwerem. Spróbuj ponownie.');
    } finally {
        hideLoadingState(form);
    }
}

function validateForm(formData) {
    const username = formData.get('username');
    const email = formData.get('email');
    const password = formData.get('password');
    const confirmPassword = formData.get('confirmPassword');
    const terms = formData.get('terms');
    
    // Sprawdź czy wszystkie pola są wypełnione
    if (!username || !email || !password || !confirmPassword) {
        showErrorMessage('Wszystkie pola są wymagane');
        return false;
    }
    
    // Sprawdź długość nazwy użytkownika
    if (username.length < 3) {
        showErrorMessage('Nazwa użytkownika musi mieć co najmniej 3 znaki');
        return false;
    }
    
    // Sprawdź format email
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    if (!emailRegex.test(email)) {
        showErrorMessage('Podaj prawidłowy adres e-mail');
        return false;
    }
    
    // Sprawdź długość hasła
    if (password.length < 6) {
        showErrorMessage('Hasło musi mieć co najmniej 6 znaków');
        return false;
    }
    
    // Sprawdź czy hasła się zgadzają
    if (password !== confirmPassword) {
        showErrorMessage('Hasła nie są identyczne');
        return false;
    }
    
    // Sprawdź czy zaakceptowano regulamin
    if (!terms) {
        showErrorMessage('Musisz zaakceptować regulamin');
        return false;
    }
    
    return true;
}

function validatePasswordStrength(e) {
    const password = e.target.value;
    const strengthIndicator = document.querySelector('.password-strength');
    
    // Usuń istniejący wskaźnik
    if (strengthIndicator) {
        strengthIndicator.remove();
    }
    
    if (password.length === 0) return;
    
    // Sprawdź siłę hasła
    let strength = 0;
    if (password.length >= 6) strength++;
    if (/[A-Z]/.test(password)) strength++;
    if (/[0-9]/.test(password)) strength++;
    if (/[^A-Za-z0-9]/.test(password)) strength++;
    
    // Utwórz wskaźnik siły
    const indicator = document.createElement('div');
    indicator.className = 'password-strength';
    
    if (strength < 2) {
        indicator.className += ' password-strength-weak';
        indicator.textContent = 'Hasło: słabe';
    } else if (strength < 3) {
        indicator.className += ' password-strength-medium';
        indicator.textContent = 'Hasło: średnie';
    } else {
        indicator.className += ' password-strength-strong';
        indicator.textContent = 'Hasło: silne';
    }
    
    e.target.parentNode.appendChild(indicator);
}

function validatePasswordMatch() {
    const password = document.getElementById('password').value;
    const confirmPassword = document.getElementById('confirm-password').value;
    const confirmField = document.querySelector('.confirm-password-field');
    
    // Usuń poprzednie komunikaty
    const existingMsg = confirmField.querySelector('.password-match-msg');
    if (existingMsg) {
        existingMsg.remove();
    }
    
    if (confirmPassword.length === 0) return;
    
    const message = document.createElement('div');
    message.className = 'password-match-msg text-xs mt-1';
    
    if (password === confirmPassword) {
        message.className += ' text-green-600 dark:text-green-400';
        message.textContent = '✓ Hasła są identyczne';
    } else {
        message.className += ' text-red-600 dark:text-red-400';
        message.textContent = '✗ Hasła nie są identyczne';
    }
    
    confirmField.appendChild(message);
}

function showLoadingState(form) {
    form.classList.add('register-form-loading');
    const submitButton = form.querySelector('button[type="submit"]');
    if (submitButton) {
        submitButton.disabled = true;
        submitButton.innerHTML = '<span class="button-text">Rejestrowanie...</span>';
    }
}

function hideLoadingState(form) {
    form.classList.remove('register-form-loading');
    const submitButton = form.querySelector('button[type="submit"]');
    if (submitButton) {
        submitButton.disabled = false;
        submitButton.innerHTML = '<span class="button-text">Zarejestruj się</span>';
    }
}

function showErrorMessage(message) {
    removeOldMessages();
    const form = document.getElementById('registerForm');
    const errorDiv = document.createElement('div');
    errorDiv.className = 'register-error';
    errorDiv.textContent = message;
    form.appendChild(errorDiv);
}

function showSuccessMessage(message) {
    removeOldMessages();
    const form = document.getElementById('registerForm');
    const successDiv = document.createElement('div');
    successDiv.className = 'register-success';
    successDiv.textContent = message;
    form.appendChild(successDiv);
}

function removeOldMessages() {
    const oldMessages = document.querySelectorAll('.register-error, .register-success');
    oldMessages.forEach(el => el.remove());
}
