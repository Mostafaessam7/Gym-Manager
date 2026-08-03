import { api } from '../api/apiClient.js';
import { createDataTable } from '../components/dataTable.js';
import { openModal } from '../components/modal.js';
import { renderForm, readForm } from '../components/form.js';
import { toastSuccess, toastError } from '../components/toast.js';
import { rawHtml } from '../utils/html.js';

function openSettingModal(onSaved) {
  const fields = [
    { name: 'key', label: 'Key', required: true },
    { name: 'value', label: 'Value', required: true },
    { name: 'description', label: 'Description', type: 'textarea', span2: true },
  ];
  const body = renderForm(fields);

  openModal({
    title: 'Add / Update Setting',
    wide: true,
    bodyHtml: '',
    onMount: (ctrl) => ctrl.bodyElement.appendChild(body),
    footerButtons: [
      { label: 'Cancel', className: 'btn-secondary', onClick: (ctrl) => ctrl.close() },
      {
        label: 'Save',
        className: 'btn-primary',
        onClick: async (ctrl) => {
          try {
            await api.put('/settings', readForm(body, fields));
            toastSuccess('Setting saved.');
            ctrl.close();
            onSaved();
          } catch (error) {
            toastError(error.message || 'Failed to save setting.');
          }
        },
      },
    ],
  });
}

export function render(container) {
  container.innerHTML = `
    <div class="card">
      <div class="card-header">
        <h2>Global Settings</h2>
        <button class="btn btn-primary" id="new-setting-btn">+ Add Setting</button>
      </div>
      <div id="settings-table"></div>
    </div>
  `;

  const table = createDataTable(document.getElementById('settings-table'), {
    searchable: false,
    columns: [
      { label: 'Key', key: 'key' },
      { label: 'Value', key: 'value' },
      { label: 'Description', render: (s) => s.description || rawHtml('<span class="text-muted">—</span>') },
    ],
    fetchPage: () => api.get('/settings'),
    rowActions: (setting) => [
      {
        label: 'Delete', className: 'btn-danger',
        onClick: async (row, reload) => { await api.delete(`/settings/${row.id}`); toastSuccess('Setting deleted.'); reload(); },
      },
    ],
  });

  document.getElementById('new-setting-btn').addEventListener('click', () => openSettingModal(table.refresh));
}
