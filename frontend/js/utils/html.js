const ESCAPE_MAP = { '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' };

// Escapes a value for safe interpolation into an innerHTML template literal. Use this for any
// user-supplied field (names, notes, emails, free text) that gets embedded alongside trusted markup
// (e.g. a status badge `<span>`) — plain values with no markup needs should use textContent instead.
export function escapeHtml(value) {
  return String(value ?? '').replace(/[&<>"']/g, (char) => ESCAPE_MAP[char]);
}

// Explicit opt-in wrapper marking a string as deliberately-constructed, safe-to-render markup (e.g. a status
// badge built entirely from system-controlled enum/boolean values). `createDataTable` only ever calls
// `innerHTML` on values wrapped this way — any other returned value is rendered as plain text via
// `textContent`, so a `dataTable` column's `render` function can never accidentally introduce an XSS
// sink just by returning a template-literal string. Any user-supplied text interpolated into the markup
// passed here MUST be escaped with `escapeHtml` first.
export class RawHtml {
  constructor(html) {
    this.html = html;
  }
}

export function rawHtml(html) {
  return new RawHtml(html);
}

// Returns `url` if it's safe to place in an href/src attribute, or '#' otherwise. Two distinct risks for a
// user-supplied URL (e.g. a gift-card receipt link, an uploaded document's file URL) interpolated into
// `href="${...}"`: (1) a literal `"` breaks out of the attribute and lets an attacker inject arbitrary
// attributes/markup — guarded here by escaping, same as any other interpolated value; (2) even a fully
// quote-safe value can carry a `javascript:`/`data:` scheme that executes on click with no injection needed
// at all — guarded here by only allowing the schemes a legitimate uploaded-file or receipt link can actually
// have. Relative URLs (the common case: `IFileStorageService` returns paths like `/uploads/...`) are allowed.
const SAFE_URL_SCHEMES = /^https?:\/\//i;
export function safeUrl(url) {
  const value = String(url ?? '');
  // `startsWith('/')` alone would also admit a protocol-relative URL ("//evil.example/x") — the browser
  // resolves that against whatever host follows the second slash, using the page's own scheme, which is
  // exactly the off-site navigation this function exists to block. Root-relative must have a third
  // character that isn't another '/'.
  const isRootRelative = value.startsWith('/') && !value.startsWith('//');
  if (isRootRelative || SAFE_URL_SCHEMES.test(value)) return escapeHtml(value);
  return '#';
}
