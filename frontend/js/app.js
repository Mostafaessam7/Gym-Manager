import { authStore } from './auth/authStore.js';
import { CONFIG } from './config.js';
import { registerRoute, setNotFoundHandler, startRouter, navigate, currentPath } from './router.js';
import { t, initI18n, getLocale, toggleLocale } from './i18n/index.js';

initI18n();

if (!authStore.isAuthenticated) {
  window.location.href = 'index.html';
}

const NAV_ITEMS = [
  { path: '/dashboard', labelKey: 'nav.dashboard', icon: '📊', permission: 'dashboard:view' },
  { path: '/members', labelKey: 'nav.members', icon: '🧑‍🤝‍🧑', permission: 'members:view' },
  { path: '/leads', labelKey: 'nav.crm', icon: '🎯', permission: 'crm:view' },
  { path: '/memberships', labelKey: 'nav.memberships', icon: '🪪', permission: 'memberships:view' },
  { path: '/attendance', labelKey: 'nav.attendance', icon: '✅', permission: 'attendance:view' },
  { path: '/classes', labelKey: 'nav.classes', icon: '🧘', permission: 'classes:view' },
  { path: '/trainers', labelKey: 'nav.trainers', icon: '🏋️', permission: 'trainers:view' },
  { path: '/fitness', labelKey: 'nav.fitness', icon: '🥗', permission: 'nutrition:view' },
  { path: '/staff', labelKey: 'nav.staff', icon: '🗓️', permission: 'staff:view' },
  { path: '/payments', labelKey: 'nav.payments', icon: '💳', permission: 'payments:view' },
  { path: '/invoices', labelKey: 'nav.invoices', icon: '🧾', permission: 'invoices:view' },
  { path: '/products', labelKey: 'nav.products', icon: '🛒', permission: 'products:view' },
  { path: '/gift-cards', labelKey: 'nav.giftCards', icon: '🎁', permission: 'gift-cards:view' },
  { path: '/lockers', labelKey: 'nav.lockers', icon: '🔒', permission: 'lockers:view' },
  { path: '/branches', labelKey: 'nav.branches', icon: '🏢', permission: 'branches:view' },
  { path: '/reports', labelKey: 'nav.reports', icon: '📈', permission: 'reports:view' },
  { path: '/users', labelKey: 'nav.users', icon: '🛡️', permission: 'users:view' },
  { path: '/settings', labelKey: 'nav.settings', icon: '⚙️', permission: 'settings:manage' },
];

function initTheme() {
  // The attribute may already be set by dashboard.html's early inline script (before this module even
  // loaded) — this just re-applies it (harmless no-op) and wires up the toggle.
  const stored = localStorage.getItem(CONFIG.THEME_STORAGE_KEY);
  if (stored) document.documentElement.setAttribute('data-theme', stored);

  const themeToggle = document.getElementById('theme-toggle');
  themeToggle.title = t('topbar.toggleTheme');
  themeToggle.addEventListener('click', () => {
    const current = document.documentElement.getAttribute('data-theme')
      || (window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light');
    const next = current === 'dark' ? 'light' : 'dark';
    document.documentElement.setAttribute('data-theme', next);
    localStorage.setItem(CONFIG.THEME_STORAGE_KEY, next);
  });
}

function initLanguage() {
  const langToggle = document.getElementById('lang-toggle');
  langToggle.textContent = getLocale() === 'ar' ? 'عربي' : 'EN';
  langToggle.title = t('topbar.toggleLanguage');
  langToggle.addEventListener('click', toggleLocale);
}

function initUserChip() {
  const session = authStore.session;
  document.getElementById('user-email').textContent = session?.email || '';
  document.getElementById('user-avatar').textContent = (session?.email || 'U').charAt(0).toUpperCase();

  const logoutBtn = document.getElementById('logout-btn');
  logoutBtn.textContent = t('topbar.logout');
  logoutBtn.addEventListener('click', () => {
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
      link.innerHTML = `<span class="nav-link__icon">${item.icon}</span><span>${t(item.labelKey)}</span>`;
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
  document.getElementById('page-title').textContent = active ? t(active.labelKey) : 'Gym Manager';
  document.getElementById('sidebar').classList.remove('open');
}

async function registerModuleRoutes() {
  const mainContent = document.getElementById('main-content');

  const modules = {
    '/dashboard': () => import('./modules/dashboard.js'),
    '/members': () => import('./modules/members.js'),
    '/leads': () => import('./modules/leads.js'),
    '/memberships': () => import('./modules/memberships.js'),
    '/attendance': () => import('./modules/attendance.js'),
    '/classes': () => import('./modules/classes.js'),
    '/trainers': () => import('./modules/trainers.js'),
    '/fitness': () => import('./modules/fitness.js'),
    '/staff': () => import('./modules/staff.js'),
    '/payments': () => import('./modules/payments.js'),
    '/invoices': () => import('./modules/invoices.js'),
    '/products': () => import('./modules/products.js'),
    '/gift-cards': () => import('./modules/giftCards.js'),
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
    document.getElementById('main-content').innerHTML = `<div class="empty-state">${t('common.pageNotFound')}</div>`;
  });
}

initTheme();
initLanguage();
initUserChip();
initSidebar();
await registerModuleRoutes();
startRouter();

if (!currentPath() || currentPath() === '/') navigate('/dashboard');
