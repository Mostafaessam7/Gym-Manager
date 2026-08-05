import { api } from '../api/apiClient.js';
import { createDataTable } from '../components/dataTable.js';
import { openModal } from '../components/modal.js';
import { renderForm, readForm } from '../components/form.js';
import { toastSuccess, toastError } from '../components/toast.js';
import { authStore } from '../auth/authStore.js';
import { rawHtml } from '../utils/html.js';
import { t, tStatus } from '../i18n/index.js';

async function openTrainerModal(existing, onSaved) {
  const fields = [
    { name: 'firstName', label: t('common.firstName'), value: existing?.firstName, required: true },
    { name: 'lastName', label: t('common.lastName'), value: existing?.lastName, required: true },
    { name: 'specialization', label: t('trainers.specialization'), value: existing?.specialization, required: true },
    { name: 'phoneNumber', label: t('trainers.phone'), value: existing?.phoneNumber },
    { name: 'email', label: t('trainers.email'), type: 'email', value: existing?.email },
    { name: 'bio', label: t('trainers.bio'), type: 'textarea', value: existing?.bio, span2: true },
  ];

  if (!existing) {
    const branches = await api.get('/branches');
    fields.unshift({ name: 'branchId', label: t('trainers.branch'), type: 'select', required: true, options: branches.map((b) => ({ value: b.id, label: b.name })) });
  }

  const body = renderForm(fields);

  openModal({
    title: existing ? t('trainers.editTrainer') : t('trainers.newTrainerTitle'),
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
            if (existing) await api.put(`/trainers/${existing.id}`, values);
            else await api.post('/trainers', values);
            toastSuccess(existing ? t('trainers.trainerUpdated') : t('trainers.trainerCreated'));
            ctrl.close();
            onSaved();
          } catch (error) {
            toastError(error.message || t('trainers.saveFailed'));
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
        <h2>${t('trainers.title')}</h2>
        ${authStore.hasPermission('trainers:manage') ? `<button class="btn btn-primary" id="new-trainer-btn">${t('trainers.newTrainer')}</button>` : ''}
      </div>
      <div id="trainers-table"></div>
    </div>
  `;

  const table = createDataTable(document.getElementById('trainers-table'), {
    searchable: false,
    columns: [
      { label: t('trainers.nameCol'), render: (tr) => `${tr.firstName} ${tr.lastName}` },
      { label: t('trainers.specializationCol'), key: 'specialization' },
      { label: t('trainers.phoneCol'), key: 'phoneNumber' },
      { label: t('trainers.statusCol'), render: (tr) => rawHtml(`<span class="badge badge-${tr.isActive ? 'success' : 'neutral'}">${tr.isActive ? tStatus('Active') : tStatus('Inactive')}</span>`) },
    ],
    fetchPage: () => api.get('/trainers', { includeInactive: true }),
    rowActions: authStore.hasPermission('trainers:manage') ? (trainer) => [
      { label: t('common.edit'), onClick: (row, reload) => openTrainerModal(row, reload) },
      ...(trainer.isActive ? [{
        label: t('common.deactivate'), className: 'btn-danger',
        onClick: async (row, reload) => { await api.post(`/trainers/${row.id}/deactivate`); toastSuccess(t('trainers.trainerDeactivated')); reload(); },
      }] : []),
    ] : null,
  });

  document.getElementById('new-trainer-btn')?.addEventListener('click', () => openTrainerModal(null, table.refresh));
}
