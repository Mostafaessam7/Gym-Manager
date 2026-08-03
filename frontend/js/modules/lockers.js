import { api } from '../api/apiClient.js';
import { createDataTable } from '../components/dataTable.js';
import { openModal } from '../components/modal.js';
import { renderForm, readForm } from '../components/form.js';
import { toastSuccess, toastError } from '../components/toast.js';
import { authStore } from '../auth/authStore.js';
import { rawHtml } from '../utils/html.js';

async function openNewLockerModal(onSaved) {
  const branches = await api.get('/branches');
  const fields = [
    { name: 'branchId', label: 'Branch', type: 'select', required: true, options: branches.map((b) => ({ value: b.id, label: b.name })) },
    { name: 'number', label: 'Locker Number', required: true },
  ];
  const body = renderForm(fields);

  openModal({
    title: 'New Locker',
    bodyHtml: '',
    onMount: (ctrl) => ctrl.bodyElement.appendChild(body),
    footerButtons: [
      { label: 'Cancel', className: 'btn-secondary', onClick: (ctrl) => ctrl.close() },
      {
        label: 'Create',
        className: 'btn-primary',
        onClick: async (ctrl) => {
          try {
            await api.post('/lockers', readForm(body, fields));
            toastSuccess('Locker created.');
            ctrl.close();
            onSaved();
          } catch (error) {
            toastError(error.message || 'Failed to create locker.');
          }
        },
      },
    ],
  });
}

async function openAssignModal(locker, onSaved) {
  const members = await api.get('/members', { pageSize: 100 });
  const fields = [
    { name: 'memberId', label: 'Member', type: 'select', required: true, options: members.items.map((m) => ({ value: m.id, label: `${m.firstName} ${m.lastName}` })) },
  ];
  const body = renderForm(fields);

  openModal({
    title: `Assign Locker ${locker.number}`,
    bodyHtml: '',
    onMount: (ctrl) => ctrl.bodyElement.appendChild(body),
    footerButtons: [
      { label: 'Cancel', className: 'btn-secondary', onClick: (ctrl) => ctrl.close() },
      {
        label: 'Assign',
        className: 'btn-primary',
        onClick: async (ctrl) => {
          try {
            await api.post(`/lockers/${locker.id}/assign`, readForm(body, fields));
            toastSuccess('Locker assigned.');
            ctrl.close();
            onSaved();
          } catch (error) {
            toastError(error.message || 'Failed to assign locker.');
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
        <h2>Lockers</h2>
        ${authStore.hasPermission('lockers:manage') ? '<button class="btn btn-primary" id="new-locker-btn">+ New Locker</button>' : ''}
      </div>
      <div id="lockers-table"></div>
    </div>
  `;

  const statusBadge = { Available: 'success', Assigned: 'info', Maintenance: 'warning' };

  const table = createDataTable(document.getElementById('lockers-table'), {
    searchable: false,
    columns: [
      { label: 'Number', key: 'number' },
      { label: 'Status', render: (l) => rawHtml(`<span class="badge badge-${statusBadge[l.status] || 'neutral'}">${l.status}</span>`) },
    ],
    fetchPage: () => api.get('/lockers'),
    rowActions: authStore.hasPermission('lockers:manage') ? (locker) => {
      const actions = [];
      if (locker.status === 'Available') {
        actions.push({ label: 'Assign', onClick: (row, reload) => openAssignModal(row, reload) });
        actions.push({ label: 'Maintenance', onClick: async (row, reload) => { await api.post(`/lockers/${row.id}/maintenance`); toastSuccess('Locker marked under maintenance.'); reload(); } });
      }
      if (locker.status === 'Assigned') {
        actions.push({ label: 'Release', className: 'btn-danger', onClick: async (row, reload) => { await api.post(`/lockers/${row.id}/release`); toastSuccess('Locker released.'); reload(); } });
      }
      return actions;
    } : null,
  });

  document.getElementById('new-locker-btn')?.addEventListener('click', () => openNewLockerModal(table.refresh));
}
