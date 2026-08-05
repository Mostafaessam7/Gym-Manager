import { api } from '../api/apiClient.js';
import { createDataTable } from '../components/dataTable.js';
import { openModal } from '../components/modal.js';
import { renderForm, readForm } from '../components/form.js';
import { toastSuccess, toastError } from '../components/toast.js';
import { authStore } from '../auth/authStore.js';
import { rawHtml } from '../utils/html.js';
import { t, tStatus } from '../i18n/index.js';

function branchFields(branch = {}) {
  return [
    { name: 'name', label: t('branches.branchName'), value: branch.name, required: true },
    { name: 'country', label: t('branches.country'), value: branch.country, required: true },
    { name: 'street', label: t('branches.street'), value: branch.street },
    { name: 'city', label: t('branches.city'), value: branch.city },
    { name: 'state', label: t('branches.state'), value: branch.state },
    { name: 'postalCode', label: t('branches.postalCode'), value: branch.postalCode },
    { name: 'phoneNumber', label: t('branches.phone'), value: branch.phoneNumber },
    { name: 'email', label: t('branches.email'), type: 'email', value: branch.email },
  ];
}

function openBranchModal(existing, onSaved) {
  const fields = branchFields(existing || {});
  const body = renderForm(fields);

  openModal({
    title: existing ? t('branches.editBranch') : t('branches.newBranchTitle'),
    wide: true,
    bodyHtml: '',
    onMount: (ctrl) => ctrl.bodyElement.appendChild(body),
    footerButtons: [
      { label: t('common.cancel'), className: 'btn-secondary', onClick: (ctrl) => ctrl.close() },
      {
        label: t('common.save'),
        className: 'btn-primary',
        onClick: async (ctrl) => {
          const values = readForm(body, fields);
          try {
            if (existing) await api.put(`/branches/${existing.id}`, values);
            else await api.post('/branches', values);
            toastSuccess(existing ? t('branches.branchUpdated') : t('branches.branchCreated'));
            ctrl.close();
            onSaved();
          } catch (error) {
            toastError(error.message || t('branches.saveFailed'));
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
        <h2>${t('branches.title')}</h2>
        ${authStore.hasPermission('branches:manage') ? `<button class="btn btn-primary" id="new-branch-btn">${t('branches.newBranch')}</button>` : ''}
      </div>
      <div id="branches-table"></div>
    </div>
  `;

  const table = createDataTable(document.getElementById('branches-table'), {
    searchable: false,
    columns: [
      { label: t('branches.name'), key: 'name' },
      { label: t('branches.cityCol'), key: 'city' },
      { label: t('branches.countryCol'), key: 'country' },
      { label: t('branches.phoneCol'), key: 'phoneNumber' },
      { label: t('branches.statusCol'), render: (b) => rawHtml(`<span class="badge badge-${b.isActive ? 'success' : 'neutral'}">${b.isActive ? tStatus('Active') : tStatus('Inactive')}</span>`) },
    ],
    fetchPage: () => api.get('/branches', { includeInactive: true }),
    rowActions: authStore.hasPermission('branches:manage') ? (branch) => [
      { label: t('common.edit'), onClick: (row, reload) => openBranchModal(row, reload) },
      ...(branch.isActive ? [{
        label: t('common.deactivate'), className: 'btn-danger',
        onClick: async (row, reload) => { await api.post(`/branches/${row.id}/deactivate`); toastSuccess(t('branches.branchDeactivated')); reload(); },
      }] : []),
    ] : null,
  });

  document.getElementById('new-branch-btn')?.addEventListener('click', () => openBranchModal(null, table.refresh));
}
