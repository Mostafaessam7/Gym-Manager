import { authStore } from './auth/authStore.js';
import { CONFIG } from './config.js';
import { registerRoute, setNotFoundHandler, startRouter, navigate, currentPath } from './router.js';

if (!authStore.isAuthenticated) {
  window.location.href = 'index.html';
}

const NAV_ITEMS = [
  { path: '/dashboard', label: 'Dashboard', icon: '📊', permission: 'dashboard:view' },
  { path: '/members', label: 'Members', icon: '🧑‍🤝‍🧑', permission: 'members:view' },
  { path: '/memberships', label: 'Memberships', icon: '🪪', permission: 'memberships:view' },
  { path: '/attendance', label: 'Attendance', icon: '✅', permission: 'attendance:view' },
  { path: '/classes', label: 'Classes', icon: '🧘', permission: 'classes:view' },
  { path: '/trainers', label: 'Trainers', icon: '🏋️', permission: 'trainers:view' },
  { path: '/payments', label: 'Payments', icon: '💳', permission: 'payments:view' },
  { path: '/invoices', label: 'Invoices', icon: '🧾', permission: 'invoices:view' },
  { path: '/products', label: 'Products & POS', icon: '🛒', permission: 'products:view' },
  { path: '/lockers', label: 'Lockers', icon: '🔒', permission: 'lockers:view' },
  { path: '/branches', label: 'Branches', icon: '🏢', permission: 'branches:view' },
  { path: '/reports', label: 'Reports', icon: '📈', permission: 'reports:view' },
  { path: '/users', label: 'Users & Roles', icon: '🛡️', permission: 'users:view' },
  { path: '/settings', label: 'Settings', icon: '⚙️', permission: 'settings:manage' },
];

function initTheme() {
  const stored = localStorage.getItem(CONFIG.THEME_STORAGE_KEY);
  if (stored) document.documentElement.setAttribute('data-theme', stored);

  document.getElementById('theme-toggle').addEventListener('click', () => {
    const current = document.documentElement.getAttribute('data-theme')
      || (window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light');
    const next = current === 'dark' ? 'light' : 'dark';
    document.documentElement.setAttribute('data-theme', next);
    localStorage.setItem(CONFIG.THEME_STORAGE_KEY, next);
  });
}

function initUserChip() {
  const session = authStore.session;
  document.getElementById('user-email').textContent = session?.email || '';
  document.getElementById('user-avatar').textContent = (session?.email || 'U').charAt(0).toUpperCase();

  document.getElementById('logout-btn').addEventListener('click', () => {
    authStore.clear();
    window.location.href = 'index.html';
  });
}

function initSidebar() {
  const nav = document.getElementById('sidebar-nav');
  nav.innerHTML = '';

  NAV_ITEMS
    .filter((item) => authStore.hasPermission(item.permission))
    .forEach((item) => {
      const link = document.createElement('div');
      link.className = 'nav-link';
      link.dataset.path = item.path;
      link.innerHTML = `<span class="nav-link__icon">${item.icon}</span><span>${item.label}</span>`;
      link.addEventListener('click', () => navigate(item.path));
      nav.appendChild(link);
    });

  const menuToggle = document.getElementById('menu-toggle');
  const sidebar = document.getElementById('sidebar');
  menuToggle.style.display = 'inline-flex';
  menuToggle.addEventListener('click', () => sidebar.classList.toggle('open'));
}

function highlightActiveNav(path) {
  document.querySelectorAll('.nav-link').forEach((el) => {
    el.classList.toggle('active', path.startsWith(el.dataset.path));
  });

  const active = NAV_ITEMS.find((item) => path.startsWith(item.path));
  document.getElementById('page-title').textContent = active?.label || 'Gym Manager';
  document.getElementById('sidebar').classList.remove('open');
}

async function registerModuleRoutes() {
  const mainContent = document.getElementById('main-content');

  const modules = {
    '/dashboard': () => import('./modules/dashboard.js'),
    '/members': () => import('./modules/members.js'),
    '/memberships': () => import('./modules/memberships.js'),
    '/attendance': () => import('./modules/attendance.js'),
    '/classes': () => import('./modules/classes.js'),
    '/trainers': () => import('./modules/trainers.js'),
    '/payments': () => import('./modules/payments.js'),
    '/invoices': () => import('./modules/invoices.js'),
    '/products': () => import('./modules/products.js'),
    '/lockers': () => import('./modules/lockers.js'),
    '/branches': () => import('./modules/branches.js'),
    '/reports': () => import('./modules/reports.js'),
    '/users': () => import('./modules/users.js'),
    '/settings': () => import('./modules/settings.js'),
  };

  Object.entries(modules).forEach(([path, loader]) => {
    registerRoute(path, async (fullPath) => {
      highlightActiveNav(fullPath);
      mainContent.innerHTML = '<div class="spinner"></div>';
      const module = await loader();
      return module.render(mainContent, fullPath);
    });
  });

  setNotFoundHandler(() => {
    document.getElementById('main-content').innerHTML = '<div class="empty-state">Page not found.</div>';
  });
}

initTheme();
initUserChip();
initSidebar();
await registerModuleRoutes();
startRouter();

if (!currentPath() || currentPath() === '/') navigate('/dashboard');
