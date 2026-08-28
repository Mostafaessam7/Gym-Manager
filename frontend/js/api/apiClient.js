import { CONFIG } from '../config.js';
import { authStore } from '../auth/authStore.js';

export class ApiError extends Error {
  constructor(message, status, problem) {
    super(message);
    this.status = status;
    this.problem = problem;
  }
}

let refreshPromise = null;

// The CSRF token is the one cookie meant to be script-readable: the server sets it non-HttpOnly and
// the client echoes it back in a header. An attacker's page can make the browser send cookies
// cross-origin but cannot read them, so it cannot produce the matching header.
function getCsrfToken() {
  const match = document.cookie.match(/(?:^|;\s*)XSRF-TOKEN=([^;]*)/);
  return match ? decodeURIComponent(match[1]) : null;
}

async function refreshAccessToken() {
  // There is no readable refresh token to check for any more — it lives in an HttpOnly cookie.
  // Attempt the refresh and let failure mean "no session", which callers already handle.
  const csrf = getCsrfToken();

  const response = await fetch(`${CONFIG.API_BASE_URL}/auth/refresh`, {
    method: 'POST',
    // Required for the browser to send and accept the cookie. Without it the cookie is silently
    // ignored and every refresh fails with no obvious cause.
    credentials: 'include',
    headers: {
      'Content-Type': 'application/json',
      'X-Auth-Transport': 'cookie',
      ...(csrf ? { 'X-XSRF-TOKEN': csrf } : {}),
    },
    body: JSON.stringify({}),
  });

  if (!response.ok) {
    authStore.clear();
    throw new ApiError('Session expired', 401);
  }

  const data = await response.json();
  authStore.setSession(toSession(data));
  return data.accessToken;
}

function toSession(authResponse) {
  return {
    userId: authResponse.userId,
    email: authResponse.email,
    accessToken: authResponse.accessToken,
    refreshToken: authResponse.refreshToken,
    accessTokenExpiresOnUtc: authResponse.accessTokenExpiresOnUtc,
    roles: authResponse.roles ?? [],
    permissions: authResponse.permissions ?? [],
  };
}

function buildUrl(path, query) {
  const url = new URL(`${CONFIG.API_BASE_URL}${path}`);
  if (query) {
    Object.entries(query).forEach(([key, value]) => {
      if (value === undefined || value === null || value === '') return;
      url.searchParams.set(key, value);
    });
  }
  return url.toString();
}

async function request(method, path, { query, body, isForm, retry = true, expectBlob = false } = {}) {
  const headers = {};
  if (!isForm) headers['Content-Type'] = 'application/json';

  if (authStore.accessToken) headers.Authorization = `Bearer ${authStore.accessToken}`;

  const response = await fetch(buildUrl(path, query), {
    method,
    // Sends the auth cookies on same-site and cross-origin calls alike. Ordinary API calls carry
    // the access token in the Authorization header as before; this matters for the auth endpoints,
    // where the refresh cookie has to travel.
    credentials: 'include',
    headers,
    body: isForm ? body : body !== undefined ? JSON.stringify(body) : undefined,
  });

  // Retry on 401 whenever the app believes it has a session. The refresh token is no longer
  // readable, so its presence cannot be checked first — the refresh attempt itself is the check,
  // and its failure lands in the catch below.
  if (response.status === 401 && retry && authStore.hasStoredProfile) {
    try {
      await (refreshPromise ??= refreshAccessToken().finally(() => (refreshPromise = null)));
      return request(method, path, { query, body, isForm, retry: false, expectBlob });
    } catch {
      window.location.hash = '#/login';
      throw new ApiError('Session expired, please sign in again.', 401);
    }
  }

  if (response.status === 204) return null;

  if (!response.ok) {
    let problem = null;
    try {
      problem = await response.json();
    } catch {
      /* body wasn't JSON */
    }
    throw new ApiError(problem?.detail || problem?.title || `Request failed (${response.status})`, response.status, problem);
  }

  if (expectBlob) return response.blob();

  const text = await response.text();
  return text ? JSON.parse(text) : null;
}

export const api = {
  get: (path, query) => request('GET', path, { query }),
  getBlob: (path, query) => request('GET', path, { query, expectBlob: true }),
  post: (path, body) => request('POST', path, { body }),
  postForm: (path, formData) => request('POST', path, { body: formData, isForm: true }),
  put: (path, body) => request('PUT', path, { body }),
  delete: (path) => request('DELETE', path, {}),
};

/**
 * Re-establishes the session after a page reload, where the in-memory access token is gone but the
 * HttpOnly refresh cookie survives.
 *
 * Resolves to true/false rather than throwing: "no valid cookie" is the ordinary logged-out case,
 * not an error worth blocking app startup over.
 */
export async function restoreSession() {
  try {
    await refreshAccessToken();
    return true;
  } catch {
    return false;
  }
}

export { toSession };
