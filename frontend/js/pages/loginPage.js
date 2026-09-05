import { CONFIG } from '../config.js';
import { authStore } from '../auth/authStore.js';
import { toSession } from '../api/apiClient.js';
import { t, initI18n, getLocale, toggleLocale } from '../i18n/index.js';

initI18n();

if (authStore.isAuthenticated) {
  window.location.href = 'dashboard.html';
}

document.title = `${t('login.signIn')} · Gym Manager`;
document.getElementById('login-email-label').textContent = t('login.email');
document.getElementById('login-password-label').textContent = t('login.password');
document.getElementById('login-hint').textContent = t('login.defaultAccount');
document.getElementById('lang-toggle').textContent = getLocale() === 'ar' ? 'ع' : 'EN';
document.getElementById('lang-toggle').title = t('topbar.toggleLanguage');
document.getElementById('theme-toggle').title = t('topbar.toggleTheme');

document.getElementById('lang-toggle').addEventListener('click', toggleLocale);
document.getElementById('theme-toggle').addEventListener('click', () => {
  const current = document.documentElement.getAttribute('data-theme')
    || (window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light');
  const next = current === 'dark' ? 'light' : 'dark';
  document.documentElement.setAttribute('data-theme', next);
  localStorage.setItem(CONFIG.THEME_STORAGE_KEY, next);
});

const form = document.getElementById('login-form');
const errorBox = document.getElementById('login-error');
const submitBtn = document.getElementById('login-submit');
submitBtn.textContent = t('login.signIn');

form.addEventListener('submit', async (event) => {
  event.preventDefault();
  errorBox.classList.add('hidden');
  submitBtn.disabled = true;
  submitBtn.textContent = t('login.signingIn');

  const email = document.getElementById('email').value.trim();
  const password = document.getElementById('password').value;

  try {
    const response = await fetch(`${CONFIG.API_BASE_URL}/auth/login`, {
      method: 'POST',
      // Opts into cookie transport: the server puts the refresh token in an HttpOnly cookie and
      // leaves it out of the response body. credentials:'include' is what lets the browser accept
      // that cookie at all.
      credentials: 'include',
      headers: {
        'Content-Type': 'application/json',
        'X-Auth-Transport': 'cookie',
      },
      body: JSON.stringify({ email, password }),
    });

    if (!response.ok) {
      const problem = await response.json().catch(() => null);
      throw new Error(problem?.detail || t('login.invalidCredentials'));
    }

    // POST /auth/login returns { requiresTwoFactor, twoFactorChallengeToken, authentication } rather than a
    // flat AuthenticationResponse (unlike /auth/refresh) — the account's tokens/roles/permissions live under
    // `authentication` and are only present when a second factor isn't required.
    const data = await response.json();
    if (data.requiresTwoFactor) {
      throw new Error(t('login.twoFactorNotSupported'));
    }

    authStore.setSession(toSession(data.authentication));
    window.location.href = 'dashboard.html';
  } catch (error) {
    errorBox.textContent = error.message || t('login.invalidCredentials');
    errorBox.classList.remove('hidden');
  } finally {
    submitBtn.disabled = false;
    submitBtn.textContent = t('login.signIn');
  }
});
