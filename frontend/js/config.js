// Central configuration. Override API_BASE_URL here if the API is hosted elsewhere.
export const CONFIG = Object.freeze({
  API_BASE_URL: window.__GYM_API_BASE_URL__ || 'http://localhost:8080/api/v1',
  TOKEN_STORAGE_KEY: 'gym.auth.tokens',
  THEME_STORAGE_KEY: 'gym.theme',
});
