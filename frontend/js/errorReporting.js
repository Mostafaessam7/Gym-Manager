// Optional Sentry error reporting.
//
// Off unless CONFIG.SENTRY_DSN is set. With no DSN nothing is downloaded, nothing initializes and
// no request leaves the browser — the page behaves exactly as it did before this file existed.
//
// Why it is loaded this way, rather than the way the other frontends do it:
//
// This frontend has no build step and no npm dependencies at all. That is deliberate, and it means
// it currently loads zero third-party code. Two obvious options both cost something real:
//
//   • Vendoring the 148 kB minified bundle into the repo drops it outside any dependency
//     management — no npm audit, no Dependabot, and updates only when somebody remembers.
//   • Loading it from a CDN unpinned hands whoever controls that CDN script execution inside an
//     authenticated gym-admin session.
//
// So it is loaded from the official CDN at a pinned version with Subresource Integrity. The
// browser verifies the hash before executing: if the CDN ever serves different bytes, the script
// is rejected rather than run. `crossorigin="anonymous"` is required for SRI to be enforced on a
// cross-origin script, and no cookies are sent with the request.
//
// Updating the version means updating INTEGRITY too — recompute with:
//   curl -sL <url> | openssl dgst -sha384 -binary | openssl base64 -A
// A mismatched hash fails closed: Sentry does not load and the app carries on without it.

import { CONFIG } from './config.js';

const VERSION = '10.72.0';
const SRC = `https://browser.sentry-cdn.com/${VERSION}/bundle.tracing.min.js`;
const INTEGRITY = 'sha384-jw6S2PDdJKrirsZq4Dz8L0J1o3fyoZUAMkYg3sErU2aKKzgnkFto63KFAHQg73i8';

function loadScript() {
  return new Promise((resolve, reject) => {
    const script = document.createElement('script');
    script.src = SRC;
    script.integrity = INTEGRITY;
    script.crossOrigin = 'anonymous';
    script.async = true;
    script.onload = () => resolve();
    script.onerror = () => reject(new Error('Sentry script failed to load or failed its integrity check.'));
    document.head.appendChild(script);
  });
}

export async function initErrorReporting() {
  const dsn = CONFIG.SENTRY_DSN;

  if (!dsn) {
    return;
  }

  try {
    await loadScript();

    window.Sentry.init({
      dsn,
      release: VERSION,

      // The default (1.0) sends a performance trace for every transaction, which exhausts the
      // quota on real traffic and then starts silently dropping the errors too — the part actually
      // worth having.
      tracesSampleRate: 0.1,

      // Members' names, phone numbers and health data are on these screens. None of it goes to a
      // third party.
      sendDefaultPii: false
    });
  } catch (error) {
    // Error reporting failing must never take the app down with it. This is the one place where
    // swallowing is right: the feature is diagnostics, and the page still works without it.
    console.warn('Error reporting is unavailable:', error.message);
  }
}
