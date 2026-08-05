import { api } from '../api/apiClient.js';
import { createDataTable } from '../components/dataTable.js';
import { toastSuccess, toastError } from '../components/toast.js';
import { authStore } from '../auth/authStore.js';
import { rawHtml } from '../utils/html.js';
import { t, tStatus } from '../i18n/index.js';

export function render(container) {
  container.innerHTML = `
    ${authStore.hasPermission('attendance:check-in') ? `
      <div class="card" style="margin-bottom: var(--spacing-5);">
        <div class="card-header"><h3>${t('attendance.checkIn')}</h3></div>
        <div class="form-grid">
          <div class="form-field">
            <label for="checkin-code">${t('attendance.scanCode')}</label>
            <input type="text" id="checkin-code" placeholder="${t('attendance.scanPlaceholder')}" />
          </div>
        </div>
        <div style="margin-top: var(--spacing-4);">
          <button class="btn btn-primary" id="checkin-btn">${t('attendance.checkIn')}</button>
        </div>
      </div>
    ` : ''}

    <div class="card">
      <div class="card-header"><h2>${t('attendance.records')}</h2></div>
      <div id="attendance-table"></div>
    </div>
  `;

  const table = createDataTable(document.getElementById('attendance-table'), {
    searchable: false,
    columns: [
      { label: t('attendance.member'), key: 'memberFullName' },
      { label: t('attendance.method'), render: (r) => tStatus(r.method) },
      { label: t('attendance.checkInCol'), render: (r) => new Date(r.checkInUtc).toLocaleString() },
      { label: t('attendance.checkOutCol'), render: (r) => r.checkOutUtc ? new Date(r.checkOutUtc).toLocaleString() : rawHtml(`<span class="badge badge-warning">${tStatus('Open')}</span>`) },
    ],
    fetchPage: (params) => api.get('/attendance', params),
  });

  document.getElementById('checkin-btn')?.addEventListener('click', async () => {
    const code = document.getElementById('checkin-code').value.trim();
    if (!code) return;

    try {
      await api.post('/attendance/check-in', { checkInCode: code, method: 0 });
      toastSuccess(t('attendance.checkedIn'));
      document.getElementById('checkin-code').value = '';
      table.refresh();
    } catch (error) {
      toastError(error.message || t('attendance.checkInFailed'));
    }
  });
}
