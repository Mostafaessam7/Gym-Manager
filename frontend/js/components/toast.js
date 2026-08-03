let container = null;

function ensureContainer() {
  if (container) return container;
  container = document.createElement('div');
  container.className = 'toast-stack';
  document.body.appendChild(container);
  return container;
}

export function showToast(message, variant = 'default', durationMs = 4000) {
  const stack = ensureContainer();
  const toast = document.createElement('div');
  toast.className = `toast${variant !== 'default' ? ` toast-${variant}` : ''}`;
  toast.textContent = message;
  stack.appendChild(toast);

  setTimeout(() => {
    toast.style.opacity = '0';
    toast.style.transition = 'opacity 200ms ease';
    setTimeout(() => toast.remove(), 200);
  }, durationMs);
}

export const toastSuccess = (message) => showToast(message, 'success');
export const toastError = (message) => showToast(message, 'danger');
export const toastWarning = (message) => showToast(message, 'warning');
