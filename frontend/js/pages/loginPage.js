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

    const data = await response.json();
    authStore.setSession(toSession(data));
    window.location.href = 'dashboard.html';
  } catch (error) {
    errorBox.textContent = error.message || 'Unable to sign in. Please try again.';
    errorBox.classList.remove('hidden');
  } finally {
    submitBtn.disabled = false;
    submitBtn.textContent = 'Sign in';
  }
});
