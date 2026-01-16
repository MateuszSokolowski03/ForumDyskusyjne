class Auth {
    constructor() {
        this.currentUser = null;
        this.promise = new Promise(resolve => {
            this.resolvePromise = resolve;
        });
        this.init();
    }

    async init() {
        this.currentUser = await this.fetchCurrentUser();
        this.resolvePromise(this.currentUser);
    }

    getUser() {
        return this.currentUser;
    }

    waitForUser() {
        return this.promise;
    }

    async fetchCurrentUser() {
        try {
            const response = await fetch('/api/auth/status', {
                method: 'GET',
                credentials: 'include',
                headers: {
                    'Content-Type': 'application/json',
                    'Accept': 'application/json'
                }
            });

            if (response.status === 403) {
                console.log('🚫 Użytkownik zablokowany! Wylogowywanie...');
                try {
                    await fetch('/api/auth/logout', { 
                        method: 'POST',
                        credentials: 'include'
                    });
                } catch (e) {
                    console.error('Błąd wylogowywania:', e);
                }
                document.cookie = 'user_session=; expires=Thu, 01 Jan 1970 00:00:00 UTC; path=/;';
                window.location.href = '/login.html?banned=1';
                return null;
            }

            if (response.status === 401) {
                return null;
            }

            if (!response.ok) {
                return null;
            }

            return await response.json();
        } catch (error) {
            console.error('❌ Błąd sprawdzania statusu:', error);
            return null;
        }
    }
}

window.auth = new Auth();
