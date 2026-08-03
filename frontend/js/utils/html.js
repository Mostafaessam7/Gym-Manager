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
