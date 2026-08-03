import { api } from '../api/apiClient.js';
import { createDataTable } from '../components/dataTable.js';
import { openModal } from '../components/modal.js';
import { renderForm, readForm } from '../components/form.js';
import { toastSuccess, toastError } from '../components/toast.js';
import { authStore } from '../auth/authStore.js';
import { rawHtml } from '../utils/html.js';

async function openTrainerModal(existing, onSaved) {
  const fields = [
    { name: 'firstName', label: 'First Name', value: existing?.firstName, required: true },
    { name: 'lastName', label: 'Last Name', value: existing?.lastName, required: true },
    { name: 'specialization', label: 'Specialization', value: existing?.specialization, required: true },
    { name: 'phoneNumber', label: 'Phone Number', value: existing?.phoneNumber },
    { name: 'email', label: 'Email', type: 'email', value: existing?.email },
    { name: 'bio', label: 'Bio', type: 'textarea', value: existing?.bio, span2: true },
  ];

  if (!existing) {
    const branches = await api.get('/branches');
    fields.unshift({ name: 'branchId', label: 'Branch', type: 'select', required: true, options: branches.map((b) => ({ value: b.id, label: b.name })) });
  }

  const body = renderForm(fields);

  openModal({
    title: existing ? 'Edit Trainer' : 'New Trainer',
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
            if (existing) await api.put(`/trainers/${existing.id}`, values);
            else await api.post('/trainers', values);
            toastSuccess(`Trainer ${existing ? 'updated' : 'created'}.`);
            ctrl.close();
            onSaved();
          } catch (error) {
            toastError(error.message || 'Failed to save trainer.');
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
        <h2>Trainers</h2>
        ${authStore.hasPermission('trainers:manage') ? '<button class="btn btn-primary" id="new-trainer-btn">+ New Trainer</button>' : ''}
      </div>
      <div id="trainers-table"></div>
    </div>
  `;

  const table = createDataTable(document.getElementById('trainers-table'), {
    searchable: false,
    columns: [
      { label: 'Name', render: (t) => `${t.firstName} ${t.lastName}` },
      { label: 'Specialization', key: 'specialization' },
      { label: 'Phone', key: 'phoneNumber' },
      { label: 'Status', render: (t) => rawHtml(`<span class="badge badge-${t.isActive ? 'success' : 'neutral'}">${t.isActive ? 'Active' : 'Inactive'}</span>`) },
    ],
    fetchPage: () => api.get('/trainers', { includeInactive: true }),
    rowActions: authStore.hasPermission('trainers:manage') ? (trainer) => [
      { label: 'Edit', onClick: (row, reload) => openTrainerModal(row, reload) },
      ...(trainer.isActive ? [{
        label: 'Deactivate', className: 'btn-danger',
        onClick: async (row, reload) => { await api.post(`/trainers/${row.id}/deactivate`); toastSuccess('Trainer deactivated.'); reload(); },
      }] : []),
    ] : null,
  });

  document.getElementById('new-trainer-btn')?.addEventListener('click', () => openTrainerModal(null, table.refresh));
}
