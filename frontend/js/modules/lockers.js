import { api } from '../api/apiClient.js';
import { createDataTable } from '../components/dataTable.js';
import { openModal } from '../components/modal.js';
import { renderForm, readForm } from '../components/form.js';
import { toastSuccess, toastError } from '../components/toast.js';
import { authStore } from '../auth/authStore.js';
import { rawHtml } from '../utils/html.js';
import { t, tStatus } from '../i18n/index.js';

async function openNewLockerModal(onSaved) {
  const branches = await api.get('/branches');
  const fields = [
    { name: 'branchId', label: t('lockers.branch'), type: 'select', required: true, options: branches.map((b) => ({ value: b.id, label: b.name })) },
    { name: 'number', label: t('lockers.lockerNumber'), required: true },
  ];
  const body = renderForm(fields);

  openModal({
    title: t('lockers.newLockerTitle'),
    bodyHtml: '',
    onMount: (ctrl) => ctrl.bodyElement.appendChild(body),
    footerButtons: [
      { label: t('common.cancel'), className: 'btn-secondary', onClick: (ctrl) => ctrl.close() },
      {
        label: t('lockers.create'),
        className: 'btn-primary',
        onClick: async (ctrl) => {
          try {
            await api.post('/lockers', readForm(body, fields));
            toastSuccess(t('lockers.lockerCreated'));
            ctrl.close();
            onSaved();
          } catch (error) {
            toastError(error.message || t('lockers.createFailed'));
          }
        },
      },
    ],
  });
}

async function openAssignModal(locker, onSaved) {
  const members = await api.get('/members', { pageSize: 100 });
  const fields = [
    { name: 'memberId', label: t('lockers.member'), type: 'select', required: true, options: members.items.map((m) => ({ value: m.id, label: `${m.firstName} ${m.lastName}` })) },
  ];
  const body = renderForm(fields);

  openModal({
    title: t('lockers.assignLockerTitle', { number: locker.number }),
    bodyHtml: '',
    onMount: (ctrl) => ctrl.bodyElement.appendChild(body),
    footerButtons: [
      { label: t('common.cancel'), className: 'btn-secondary', onClick: (ctrl) => ctrl.close() },
      {
        label: t('lockers.assign'),
        className: 'btn-primary',
        onClick: async (ctrl) => {
          try {
            await api.post(`/lockers/${locker.id}/assign`, readForm(body, fields));
            toastSuccess(t('lockers.lockerAssigned'));
            ctrl.close();
            onSaved();
          } catch (error) {
            toastError(error.message || t('lockers.assignFailed'));
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
        <h2>${t('lockers.title')}</h2>
        ${authStore.hasPermission('lockers:manage') ? `<button class="btn btn-primary" id="new-locker-btn">${t('lockers.newLocker')}</button>` : ''}
      </div>
      <div id="lockers-table"></div>
    </div>
  `;

  const statusBadge = { Available: 'success', Assigned: 'info', Maintenance: 'warning' };

  const table = createDataTable(document.getElementById('lockers-table'), {
    searchable: false,
    columns: [
      { label: t('lockers.numberCol'), key: 'number' },
      { label: t('lockers.statusCol'), render: (l) => rawHtml(`<span class="badge badge-${statusBadge[l.status] || 'neutral'}">${tStatus(l.status)}</span>`) },
    ],
    fetchPage: () => api.get('/lockers'),
    rowActions: authStore.hasPermission('lockers:manage') ? (locker) => {
      const actions = [];
      if (locker.status === 'Available') {
        actions.push({ label: t('lockers.assign'), onClick: (row, reload) => openAssignModal(row, reload) });
        actions.push({ label: t('lockers.maintenance'), onClick: async (row, reload) => { await api.post(`/lockers/${row.id}/maintenance`); toastSuccess(t('lockers.lockerMaintenance')); reload(); } });
      }
      if (locker.status === 'Assigned') {
        actions.push({ label: t('lockers.release'), className: 'btn-danger', onClick: async (row, reload) => { await api.post(`/lockers/${row.id}/release`); toastSuccess(t('lockers.lockerReleased')); reload(); } });
      }
      return actions;
    } : null,
  });

  document.getElementById('new-locker-btn')?.addEventListener('click', () => openNewLockerModal(table.refresh));
}
