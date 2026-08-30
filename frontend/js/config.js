// Central configuration. Override API_BASE_URL here if the API is hosted elsewhere.
export const CONFIG = Object.freeze({
  API_BASE_URL: window.__GYM_API_BASE_URL__ || 'http://localhost:8080/api/v1',
  TOKEN_STORAGE_KEY: 'gym.auth.tokens',
  THEME_STORAGE_KEY: 'gym.theme',

  // Sentry error reporting. Empty means off: nothing is downloaded and no request leaves the
  // browser. Set it the same way as the API base URL — via window.__GYM_SENTRY_DSN__ injected by
  // the host page — so a DSN never has to be committed. See js/errorReporting.js.
  SENTRY_DSN: window.__GYM_SENTRY_DSN__ || '',
});
