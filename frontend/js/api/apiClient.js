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

async function refreshAccessToken() {
  const session = authStore.session;
  if (!session?.refreshToken) throw new ApiError('No refresh token available', 401);

  const response = await fetch(`${CONFIG.API_BASE_URL}/auth/refresh`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ refreshToken: session.refreshToken }),
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
    headers,
    body: isForm ? body : body !== undefined ? JSON.stringify(body) : undefined,
  });

  if (response.status === 401 && retry && authStore.session?.refreshToken) {
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

export { toSession };
