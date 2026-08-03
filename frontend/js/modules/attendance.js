import { api } from '../api/apiClient.js';
import { createDataTable } from '../components/dataTable.js';
import { toastSuccess, toastError } from '../components/toast.js';
import { authStore } from '../auth/authStore.js';
import { rawHtml } from '../utils/html.js';

export function render(container) {
  container.innerHTML = `
    ${authStore.hasPermission('attendance:check-in') ? `
      <div class="card" style="margin-bottom: var(--spacing-5);">
        <div class="card-header"><h3>Check In</h3></div>
        <div class="form-grid">
          <div class="form-field">
            <label for="checkin-code">Scan QR / Barcode Code</label>
            <input type="text" id="checkin-code" placeholder="Scan or paste check-in code" />
          </div>
        </div>
        <div style="margin-top: var(--spacing-4);">
          <button class="btn btn-primary" id="checkin-btn">Check In</button>
        </div>
      </div>
    ` : ''}

    <div class="card">
      <div class="card-header"><h2>Attendance Records</h2></div>
      <div id="attendance-table"></div>
    </div>
  `;

  const table = createDataTable(document.getElementById('attendance-table'), {
    searchable: false,
    columns: [
      { label: 'Member', key: 'memberFullName' },
      { label: 'Method', key: 'method' },
      { label: 'Check-in', render: (r) => new Date(r.checkInUtc).toLocaleString() },
      { label: 'Check-out', render: (r) => r.checkOutUtc ? new Date(r.checkOutUtc).toLocaleString() : rawHtml('<span class="badge badge-warning">Open</span>') },
    ],
    fetchPage: (params) => api.get('/attendance', params),
  });

  document.getElementById('checkin-btn')?.addEventListener('click', async () => {
    const code = document.getElementById('checkin-code').value.trim();
    if (!code) return;

    try {
      await api.post('/attendance/check-in', { checkInCode: code, method: 0 });
      toastSuccess('Member checked in.');
      document.getElementById('checkin-code').value = '';
      table.refresh();
    } catch (error) {
      toastError(error.message || 'Check-in failed.');
    }
  });
}
