import { api } from '../api/apiClient.js';
import { createDataTable } from '../components/dataTable.js';
import { openModal } from '../components/modal.js';
import { renderForm, readForm } from '../components/form.js';
import { toastSuccess, toastError } from '../components/toast.js';
import { authStore } from '../auth/authStore.js';
import { rawHtml } from '../utils/html.js';

function branchFields(branch = {}) {
  return [
    { name: 'name', label: 'Branch Name', value: branch.name, required: true },
    { name: 'country', label: 'Country', value: branch.country, required: true },
    { name: 'street', label: 'Street', value: branch.street },
    { name: 'city', label: 'City', value: branch.city },
    { name: 'state', label: 'State / Province', value: branch.state },
    { name: 'postalCode', label: 'Postal Code', value: branch.postalCode },
    { name: 'phoneNumber', label: 'Phone Number', value: branch.phoneNumber },
    { name: 'email', label: 'Email', type: 'email', value: branch.email },
  ];
}

function openBranchModal(existing, onSaved) {
  const fields = branchFields(existing);
  const body = renderForm(fields);

  openModal({
    title: existing ? 'Edit Branch' : 'New Branch',
    wide: true,
    bodyHtml: '',
    onMount: (ctrl) => ctrl.bodyElement.appendChild(body),
    footerButtons: [
      { label: 'Cancel', className: 'btn-secondary', onClick: (ctrl) => ctrl.close() },
      {
        label: 'Save',
        className: 'btn-primary',
        onClick: async (ctrl) => {
          const values = readForm(body, fields);
          try {
            if (existing) await api.put(`/branches/${existing.id}`, values);
            else await api.post('/branches', values);
            toastSuccess(`Branch ${existing ? 'updated' : 'created'}.`);
            ctrl.close();
            onSaved();
          } catch (error) {
            toastError(error.message || 'Failed to save branch.');
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
        <h2>Branches</h2>
        ${authStore.hasPermission('branches:manage') ? '<button class="btn btn-primary" id="new-branch-btn">+ New Branch</button>' : ''}
      </div>
      <div id="branches-table"></div>
    </div>
  `;

  const table = createDataTable(document.getElementById('branches-table'), {
    searchable: false,
    columns: [
      { label: 'Name', key: 'name' },
      { label: 'City', key: 'city' },
      { label: 'Country', key: 'country' },
      { label: 'Phone', key: 'phoneNumber' },
      { label: 'Status', render: (b) => rawHtml(`<span class="badge badge-${b.isActive ? 'success' : 'neutral'}">${b.isActive ? 'Active' : 'Inactive'}</span>`) },
    ],
    fetchPage: () => api.get('/branches', { includeInactive: true }),
    rowActions: authStore.hasPermission('branches:manage') ? (branch) => [
      { label: 'Edit', onClick: (row, reload) => openBranchModal(row, reload) },
      ...(branch.isActive ? [{
        label: 'Deactivate', className: 'btn-danger',
        onClick: async (row, reload) => { await api.post(`/branches/${row.id}/deactivate`); toastSuccess('Branch deactivated.'); reload(); },
      }] : []),
    ] : null,
  });

  document.getElementById('new-branch-btn')?.addEventListener('click', () => openBranchModal(null, table.refresh));
}
