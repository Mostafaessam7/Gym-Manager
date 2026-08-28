import { CONFIG } from '../config.js';

// Holds the current session.
//
// The access token lives ONLY in memory — never localStorage, never sessionStorage. The refresh
// token is not held here at all: it is in an HttpOnly cookie the browser attaches automatically and
// JavaScript cannot read. Previously the whole session, refresh token included, was persisted to
// localStorage, where any injected script could read it. A refresh token is a renewable session, so
// reading it once meant minting access tokens indefinitely. This frontend has no build step, which
// makes Web Storage an especially cheap target.
//
// The non-sensitive parts (email, roles, permissions) are still persisted so the shell can render
// menus immediately on reload instead of flashing an empty UI while the silent refresh completes.
// They are NOT credentials and are never trusted for authorization: every API call is checked by
// the server independently, so tampering with them changes what this browser draws and nothing the
// server honours.
class AuthStore extends EventTarget {
  constructor() {
    super();
    this._accessToken = null;
    this._profile = this._loadProfile();
  }

  _loadProfile() {
    try {
      const raw = localStorage.getItem(CONFIG.TOKEN_STORAGE_KEY);
      if (!raw) return null;

      const parsed = JSON.parse(raw);

      // A browser upgrading from the previous build still has tokens sitting in localStorage.
      // Strip them on first read rather than leaving a live credential there indefinitely.
      if (parsed.accessToken || parsed.refreshToken) {
        delete parsed.accessToken;
        delete parsed.refreshToken;
        localStorage.setItem(CONFIG.TOKEN_STORAGE_KEY, JSON.stringify(parsed));
      }

      return parsed;
    } catch {
      return null;
    }
  }

  get session() {
    return this._profile;
  }

  // True only with a live access token in memory. A persisted profile alone is not a session: after
  // a reload the app must complete its silent refresh before it can call anything.
  get isAuthenticated() {
    return !!this._accessToken;
  }

  get accessToken() {
    return this._accessToken;
  }

  // Survives a reload, so the shell can draw the right menus before the refresh completes.
  get hasStoredProfile() {
    return !!this._profile;
  }

  get permissions() {
    return this._profile?.permissions ?? [];
  }

  hasPermission(code) {
    return this.permissions.includes(code);
  }

  setSession(session) {
    const { accessToken, refreshToken, ...profile } = session;

    this._accessToken = accessToken ?? null;
    this._profile = profile;

    // Deliberately excludes both tokens — see the note at the top of this file.
    localStorage.setItem(CONFIG.TOKEN_STORAGE_KEY, JSON.stringify(profile));
    this.dispatchEvent(new CustomEvent('change'));
  }

  clear() {
    this._accessToken = null;
    this._profile = null;
    localStorage.removeItem(CONFIG.TOKEN_STORAGE_KEY);
    this.dispatchEvent(new CustomEvent('change'));
  }
}

export const authStore = new AuthStore();
