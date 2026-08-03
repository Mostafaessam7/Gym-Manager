const routes = new Map();
let notFoundHandler = () => {};
let currentCleanup = null;

export function registerRoute(path, handler) {
  routes.set(path, handler);
}

export function setNotFoundHandler(handler) {
  notFoundHandler = handler;
}

export function navigate(path) {
  window.location.hash = path;
}

export function currentPath() {
  return window.location.hash.replace(/^#/, '') || '/dashboard';
}

async function resolve() {
  if (typeof currentCleanup === 'function') {
    try { currentCleanup(); } catch { /* ignore cleanup errors */ }
    currentCleanup = null;
  }

  const path = currentPath();
  const basePath = '/' + (path.split('/')[1] || 'dashboard');
  const handler = routes.get(basePath) || notFoundHandler;

  const result = await handler(path);
  if (typeof result === 'function') currentCleanup = result;
}

export function startRouter() {
  window.addEventListener('hashchange', resolve);
  resolve();
}
