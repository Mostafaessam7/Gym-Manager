import { escapeHtml } from '../utils/html.js';

// Reusable modal dialog. Returns a controller with .close() so callers can dismiss programmatically.
// `title` is treated as plain text (escaped) — it is a label, never markup. `bodyHtml` is trusted markup
// built by the caller (forms, etc.); callers must escape any user-supplied text they interpolate into it.
export function openModal({ title, bodyHtml, wide = false, footerButtons = [], onMount, onClose }) {
  const backdrop = document.createElement('div');
  backdrop.className = 'modal-backdrop';

  const modal = document.createElement('div');
  modal.className = `modal${wide ? ' modal-wide' : ''}`;

  const header = document.createElement('div');
  header.className = 'modal__header';
  header.innerHTML = `<h3>${escapeHtml(title)}</h3>`;

  const closeBtn = document.createElement('button');
  closeBtn.className = 'modal-close';
  closeBtn.innerHTML = '&times;';
  closeBtn.setAttribute('aria-label', 'Close');
  header.appendChild(closeBtn);

  const body = document.createElement('div');
  body.className = 'modal__body';
  body.innerHTML = bodyHtml;

  modal.appendChild(header);
  modal.appendChild(body);

  if (footerButtons.length) {
    const footer = document.createElement('div');
    footer.className = 'modal__footer';
    footerButtons.forEach((btnConfig) => {
      const btn = document.createElement('button');
      btn.className = `btn ${btnConfig.className || 'btn-secondary'}`;
      btn.textContent = btnConfig.label;
      btn.addEventListener('click', () => btnConfig.onClick?.(controller));
      footer.appendChild(btn);
    });
    modal.appendChild(footer);
  }

  backdrop.appendChild(modal);
  document.body.appendChild(backdrop);

  function close() {
    backdrop.remove();
    document.removeEventListener('keydown', onKeyDown);
    onClose?.();
  }

  function onKeyDown(event) {
    if (event.key === 'Escape') close();
  }

  backdrop.addEventListener('click', (event) => {
    if (event.target === backdrop) close();
  });
  closeBtn.addEventListener('click', close);
  document.addEventListener('keydown', onKeyDown);

  const controller = { close, bodyElement: body, backdropElement: backdrop };

  onMount?.(controller);

  return controller;
}

// `message` is treated as plain text (escaped) — callers pass a sentence, not markup.
export function confirmDialog(message, { title = 'Please confirm', confirmLabel = 'Confirm', danger = true } = {}) {
  return new Promise((resolve) => {
    openModal({
      title,
      bodyHtml: `<p>${escapeHtml(message)}</p>`,
      footerButtons: [
        { label: 'Cancel', className: 'btn-secondary', onClick: (ctrl) => { ctrl.close(); resolve(false); } },
        {
          label: confirmLabel,
          className: danger ? 'btn-danger' : 'btn-primary',
          onClick: (ctrl) => { ctrl.close(); resolve(true); },
        },
      ],
    });
  });
}
