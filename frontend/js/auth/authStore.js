import { CONFIG } from '../config.js';

// Holds the current session's tokens/claims and persists them to localStorage.
class AuthStore extends EventTarget {
  constructor() {
    super();
    this._session = this._loadSession();
  }

  _loadSession() {
    try {
      const raw = localStorage.getItem(CONFIG.TOKEN_STORAGE_KEY);
      return raw ? JSON.parse(raw) : null;
    } catch {
      return null;
    }
  }

  get session() {
    return this._session;
  }

  get isAuthenticated() {
    return !!this._session?.accessToken;
  }

  get accessToken() {
    return this._session?.accessToken ?? null;
  }

  get permissions() {
    return this._session?.permissions ?? [];
  }

  hasPermission(code) {
    return this.permissions.includes(code);
  }

  setSession(session) {
    this._session = session;
    localStorage.setItem(CONFIG.TOKEN_STORAGE_KEY, JSON.stringify(session));
    this.dispatchEvent(new CustomEvent('change'));
  }

  clear() {
    this._session = null;
    localStorage.removeItem(CONFIG.TOKEN_STORAGE_KEY);
    this.dispatchEvent(new CustomEvent('change'));
  }
}

export const authStore = new AuthStore();
