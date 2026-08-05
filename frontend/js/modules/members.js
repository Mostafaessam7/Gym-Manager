import { api } from '../api/apiClient.js';
import { createDataTable } from '../components/dataTable.js';
import { openModal, confirmDialog } from '../components/modal.js';
import { renderForm, readForm } from '../components/form.js';
import { toastSuccess, toastError } from '../components/toast.js';
import { authStore } from '../auth/authStore.js';
import { rawHtml } from '../utils/html.js';
import { t, tStatus } from '../i18n/index.js';

const STATUS_BADGE = { Active: 'success', Frozen: 'warning', Inactive: 'neutral' };

function memberFields(member = {}) {
  return [
    { name: 'firstName', label: t('common.firstName'), value: member.firstName, required: true },
    { name: 'lastName', label: t('common.lastName'), value: member.lastName, required: true },
    { name: 'phoneNumber', label: t('common.phoneNumber'), value: member.phoneNumber, required: true },
    { name: 'email', label: t('common.email'), type: 'email', value: member.email },
    { name: 'dateOfBirth', label: t('members.dob'), type: 'date', value: member.dateOfBirth },
    { name: 'gender', label: t('members.gender'), type: 'select', value: member.gender ?? 0, options: [{ value: 0, label: t('common.unspecified') }, { value: 1, label: t('common.male') }, { value: 2, label: t('common.female') }] },
    { name: 'country', label: t('members.country'), value: member.country },
    { name: 'city', label: t('members.city'), value: member.city },
    { name: 'emergencyContactName', label: t('members.emergencyContact'), value: member.emergencyContactName },
    { name: 'emergencyContactPhone', label: t('members.emergencyPhone'), value: member.emergencyContactPhone },
  ];
}

async function openMemberModal(existing, onSaved) {
  const branches = await api.get('/branches');
  const fields = memberFields(existing || {});
  if (!existing) {
    fields.unshift({
      name: 'branchId', label: t('members.branch'), type: 'select', required: true,
      options: branches.map((b) => ({ value: b.id, label: b.name })),
    });
  }

  const body = renderForm(fields);

  openModal({
    title: existing ? t('members.editMember') : t('members.newMemberTitle'),
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
            if (existing) {
              await api.put(`/members/${existing.id}`, values);
              toastSuccess(t('members.memberUpdated'));
            } else {
              await api.post('/members', { ...values, branchId: values.branchId });
              toastSuccess(t('members.memberCreated'));
            }
            ctrl.close();
            onSaved();
          } catch (error) {
            toastError(error.message || t('members.saveFailed'));
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
        <h2>${t('members.title')}</h2>
        ${authStore.hasPermission('members:create') ? `<button class="btn btn-primary" id="new-member-btn">${t('members.newMember')}</button>` : ''}
      </div>
      <div id="members-table"></div>
    </div>
  `;

  const table = createDataTable(document.getElementById('members-table'), {
    columns: [
      { label: t('members.code'), key: 'memberCode' },
      { label: t('members.name'), render: (m) => `${m.firstName} ${m.lastName}` },
      { label: t('members.phone'), key: 'phoneNumber' },
      { label: t('members.email'), render: (m) => m.email || rawHtml('<span class="text-muted">—</span>') },
      { label: t('members.statusCol'), render: (m) => rawHtml(`<span class="badge badge-${STATUS_BADGE[m.status] || 'neutral'}">${tStatus(m.status)}</span>`) },
      { label: t('members.joined'), render: (m) => new Date(m.joinedOnUtc).toLocaleDateString() },
    ],
    fetchPage: (params) => api.get('/members', params),
    rowActions: (member) => {
      const actions = [];
      if (authStore.hasPermission('members:update')) {
        actions.push({ label: t('common.edit'), onClick: (row, reload) => openMemberModal(row, reload) });
        actions.push(member.status === 'Frozen'
          ? { label: t('members.unfreeze'), onClick: async (row, reload) => { await api.post(`/members/${row.id}/unfreeze`); toastSuccess(t('members.memberUnfrozen')); reload(); } }
          : { label: t('members.freeze'), onClick: async (row, reload) => { await api.post(`/members/${row.id}/freeze`); toastSuccess(t('members.memberFrozen')); reload(); } });
      }
      if (authStore.hasPermission('members:delete')) {
        actions.push({
          label: t('common.delete'),
          className: 'btn-danger',
          onClick: async (row, reload) => {
            if (await confirmDialog(t('members.deleteConfirm', { name: `${row.firstName} ${row.lastName}` }))) {
              await api.delete(`/members/${row.id}`);
              toastSuccess(t('members.memberDeleted'));
              reload();
            }
          },
        });
      }
      return actions;
    },
  });

  document.getElementById('new-member-btn')?.addEventListener('click', () => openMemberModal(null, table.refresh));
}
