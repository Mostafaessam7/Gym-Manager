import { CONFIG } from '../config.js';
import { authStore } from '../auth/authStore.js';
import { toSession } from '../api/apiClient.js';

if (authStore.isAuthenticated) {
  window.location.href = 'dashboard.html';
}

const form = document.getElementById('login-form');
const errorBox = document.getElementById('login-error');
const submitBtn = document.getElementById('login-submit');

form.addEventListener('submit', async (event) => {
  event.preventDefault();
  errorBox.classList.add('hidden');
  submitBtn.disabled = true;
  submitBtn.textContent = 'Signing in…';

  const email = document.getElementById('email').value.trim();
  const password = document.getElementById('password').value;

  try {
    const response = await fetch(`${CONFIG.API_BASE_URL}/auth/login`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ email, password }),
    });

    if (!response.ok) {
      const problem = await response.json().catch(() => null);
      throw new Error(problem?.detail || 'Invalid email or password.');
    }

    // POST /auth/login returns { requiresTwoFactor, twoFactorChallengeToken, authentication } rather than a
    // flat AuthenticationResponse (unlike /auth/refresh) — the account's tokens/roles/permissions live under
    // `authentication` and are only present when a second factor isn't required.
    const data = await response.json();
    if (data.requiresTwoFactor) {
      throw new Error('This account has two-factor authentication enabled. The admin UI does not yet support completing a 2FA challenge — use POST /auth/login/2fa (see Swagger) to finish signing in.');
    }

    authStore.setSession(toSession(data.authentication));
    window.location.href = 'dashboard.html';
  } catch (error) {
    errorBox.textContent = error.message || 'Unable to sign in. Please try again.';
    errorBox.classList.remove('hidden');
  } finally {
    submitBtn.disabled = false;
    submitBtn.textContent = 'Sign in';
  }
});
