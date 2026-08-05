import en from './en.js';
import ar from './ar.js';

const CATALOGS = { en, ar };
const STORAGE_KEY = 'gym.locale';
const RTL_LOCALES = new Set(['ar']);

function detectInitialLocale() {
  try {
    const stored = localStorage.getItem(STORAGE_KEY);
    if (stored && CATALOGS[stored]) return stored;
  } catch { /* localStorage unavailable */ }
  return 'en';
}

let currentLocale = detectInitialLocale();

function resolve(catalog, path) {
  return path.split('.').reduce((node, segment) => (node && typeof node === 'object' ? node[segment] : undefined), catalog);
}

/**
 * Translates a dot-path key (e.g. 'members.newMember') against the active locale, falling back to the
 * English catalog and finally to the raw key itself so a missing translation degrades to readable English
 * rather than "undefined" or a blank string. `params` does simple {name}-style interpolation.
 */
export function t(key, params) {
  let value = resolve(CATALOGS[currentLocale], key);
  if (value === undefined) value = resolve(CATALOGS.en, key);
  if (value === undefined) return key;

  if (params) {
    Object.entries(params).forEach(([name, replacement]) => {
      value = value.replace(new RegExp(`\\{${name}\\}`, 'g'), replacement);
    });
  }
  return value;
}

/** Translates a backend enum/status string (e.g. Member.status = "Frozen") for display, falling back to
 * the raw value untranslated if it isn't in the status catalog — so an enum added later never breaks. */
export function tStatus(value) {
  if (value === undefined || value === null || value === '') return value;
  const translated = resolve(CATALOGS[currentLocale], `status.${value}`) ?? resolve(CATALOGS.en, `status.${value}`);
  return translated ?? value;
}

export function getLocale() {
  return currentLocale;
}

function applyDocumentAttributes(locale) {
  document.documentElement.setAttribute('lang', locale);
  document.documentElement.setAttribute('dir', RTL_LOCALES.has(locale) ? 'rtl' : 'ltr');
}

/** Call as early as possible (before first paint) on every entry-point page to avoid a flash of the wrong
 * language/direction — mirrors the dark-mode early-init pattern already used for `data-theme`. */
export function initI18n() {
  applyDocumentAttributes(currentLocale);
}

/**
 * Switches the active locale, persists it, and reloads the page. A full reload (rather than a reactive
 * re-render) is deliberate: every module here rebuilds its DOM imperatively from `render()`, so a reload
 * is the only way to guarantee every open page/modal/table is consistently retranslated instead of leaving
 * stale English text in whatever wasn't re-rendered.
 */
export function setLocale(locale) {
  if (!CATALOGS[locale]) return;
  try { localStorage.setItem(STORAGE_KEY, locale); } catch { /* localStorage unavailable */ }
  currentLocale = locale;
  applyDocumentAttributes(locale);
  window.location.reload();
}

export function toggleLocale() {
  setLocale(currentLocale === 'ar' ? 'en' : 'ar');
}

export function isRtl() {
  return RTL_LOCALES.has(currentLocale);
}
