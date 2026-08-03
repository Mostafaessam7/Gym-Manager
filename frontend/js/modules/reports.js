import { api } from '../api/apiClient.js';
import { authStore } from '../auth/authStore.js';
import { toastError } from '../components/toast.js';
import { escapeHtml } from '../utils/html.js';

const REPORTS = {
  members: { label: 'Members', path: '/reports/members', needsRange: false },
  attendance: { label: 'Attendance', path: '/reports/attendance', needsRange: true },
  revenue: { label: 'Revenue', path: '/reports/revenue', needsRange: true },
  memberships: { label: 'Memberships', path: '/reports/memberships', needsRange: false },
  trainers: { label: 'Trainers', path: '/reports/trainers', needsRange: true },
  classes: { label: 'Classes', path: '/reports/classes', needsRange: true },
  inventory: { label: 'Inventory', path: '/reports/inventory', needsRange: false },
  sales: { label: 'Sales', path: '/reports/sales', needsRange: true },
  expenses: { label: 'Expenses', path: '/reports/expenses', needsRange: true },
  'profit-and-loss': { label: 'Profit & Loss', path: '/reports/profit-and-loss', needsRange: true },
  'daily-closing': { label: 'Daily Closing', path: '/reports/daily-closing', needsRange: false, needsDate: true },
  'cash-flow': { label: 'Cash Flow', path: '/reports/cash-flow', needsRange: true },
};

function renderGenericTable(rows) {
  const list = Array.isArray(rows) ? rows : [rows];
  if (!list.length) return '<div class="empty-state">No data for the selected range.</div>';

  const columns = Object.keys(list[0]);
  return `
    <div class="data-table-wrap">
      <table class="data-table">
        <thead><tr>${columns.map((c) => `<th>${escapeHtml(c)}</th>`).join('')}</tr></thead>
        <tbody>
          ${list.map((row) => `<tr>${columns.map((c) => `<td>${escapeHtml(row[c] ?? '')}</td>`).join('')}</tr>`).join('')}
        </tbody>
      </table>
    </div>
  `;
}

export function render(container) {
  container.innerHTML = `
    <div class="card">
      <h2>Reports</h2>
      <div class="form-grid" style="margin-bottom: var(--spacing-4);">
        <div class="form-field">
          <label for="report-type">Report</label>
          <select id="report-type">${Object.entries(REPORTS).map(([key, r]) => `<option value="${key}">${r.label}</option>`).join('')}</select>
        </div>
        <div class="form-field" id="from-field"><label for="report-from">From</label><input type="date" id="report-from" /></div>
        <div class="form-field" id="to-field"><label for="report-to">To</label><input type="date" id="report-to" /></div>
        <div class="form-field" id="date-field" class="hidden"><label for="report-date">Date</label><input type="date" id="report-date" /></div>
      </div>
      <div style="display:flex; gap: var(--spacing-3); margin-bottom: var(--spacing-4);">
        <button class="btn btn-primary" id="view-btn">View</button>
        ${authStore.hasPermission('reports:export') || authStore.hasPermission('reports:view') ? `
          <button class="btn btn-secondary" id="export-pdf-btn">Export PDF</button>
          <button class="btn btn-secondary" id="export-excel-btn">Export Excel</button>
        ` : ''}
      </div>
      <div id="report-results"></div>
    </div>
  `;

  const today = new Date().toISOString().slice(0, 10);
  const monthAgo = new Date(Date.now() - 30 * 86400000).toISOString().slice(0, 10);
  document.getElementById('report-from').value = monthAgo;
  document.getElementById('report-to').value = today;
  document.getElementById('report-date').value = today;

  function buildParams() {
    const type = document.getElementById('report-type').value;
    const config = REPORTS[type];
    const params = {};
    if (config.needsRange) {
      params.from = document.getElementById('report-from').value;
      params.to = document.getElementById('report-to').value;
    }
    if (config.needsDate) {
      params.date = document.getElementById('report-date').value;
    }
    return { type, config, params };
  }

  document.getElementById('view-btn').addEventListener('click', async () => {
    const { config, params } = buildParams();
    const resultsEl = document.getElementById('report-results');
    resultsEl.innerHTML = '<div class="spinner"></div>';
    try {
      const data = await api.get(config.path, params);
      resultsEl.innerHTML = renderGenericTable(data);
    } catch (error) {
      toastError(error.message || 'Failed to load report.');
      resultsEl.innerHTML = '<div class="empty-state">Failed to load report.</div>';
    }
  });

  async function exportReport(format) {
    const { config, params } = buildParams();
    try {
      const blob = await api.getBlob(config.path, { ...params, format });
      const url = URL.createObjectURL(blob);
      const link = document.createElement('a');
      link.href = url;
      link.download = `${config.label.replace(/\s+/g, '-').toLowerCase()}.${format === 'pdf' ? 'pdf' : 'xlsx'}`;
      link.click();
      URL.revokeObjectURL(url);
    } catch (error) {
      toastError(error.message || 'Failed to export report.');
    }
  }

  document.getElementById('export-pdf-btn')?.addEventListener('click', () => exportReport('pdf'));
  document.getElementById('export-excel-btn')?.addEventListener('click', () => exportReport('excel'));
}
