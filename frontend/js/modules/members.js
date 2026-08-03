import { api } from '../api/apiClient.js';
import { createDataTable } from '../components/dataTable.js';
import { openModal, confirmDialog } from '../components/modal.js';
import { renderForm, readForm } from '../components/form.js';
import { toastSuccess, toastError } from '../components/toast.js';
import { authStore } from '../auth/authStore.js';
import { rawHtml } from '../utils/html.js';

const STATUS_BADGE = { Active: 'success', Frozen: 'warning', Inactive: 'neutral' };

function memberFields(member = {}) {
  return [
    { name: 'firstName', label: 'First Name', value: member.firstName, required: true },
    { name: 'lastName', label: 'Last Name', value: member.lastName, required: true },
    { name: 'phoneNumber', label: 'Phone Number', value: member.phoneNumber, required: true },
    { name: 'email', label: 'Email', type: 'email', value: member.email },
    { name: 'dateOfBirth', label: 'Date of Birth', type: 'date', value: member.dateOfBirth },
    { name: 'gender', label: 'Gender', type: 'select', value: member.gender ?? 0, options: [{ value: 0, label: 'Unspecified' }, { value: 1, label: 'Male' }, { value: 2, label: 'Female' }] },
    { name: 'country', label: 'Country', value: member.country },
    { name: 'city', label: 'City', value: member.city },
    { name: 'emergencyContactName', label: 'Emergency Contact', value: member.emergencyContactName },
    { name: 'emergencyContactPhone', label: 'Emergency Phone', value: member.emergencyContactPhone },
  ];
}

async function openMemberModal(existing, onSaved) {
  const branches = await api.get('/branches');
  const fields = memberFields(existing);
  if (!existing) {
    fields.unshift({
      name: 'branchId', label: 'Branch', type: 'select', required: true,
      options: branches.map((b) => ({ value: b.id, label: b.name })),
    });
  }

  const body = renderForm(fields);

  openModal({
    title: existing ? 'Edit Member' : 'New Member',
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
            if (existing) {
              await api.put(`/members/${existing.id}`, values);
              toastSuccess('Member updated.');
            } else {
              await api.post('/members', { ...values, branchId: values.branchId });
              toastSuccess('Member created.');
            }
            ctrl.close();
            onSaved();
          } catch (error) {
            toastError(error.message || 'Failed to save member.');
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
        <h2>Members</h2>
        ${authStore.hasPermission('members:create') ? '<button class="btn btn-primary" id="new-member-btn">+ New Member</button>' : ''}
      </div>
      <div id="members-table"></div>
    </div>
  `;

  const table = createDataTable(document.getElementById('members-table'), {
    columns: [
      { label: 'Code', key: 'memberCode' },
      { label: 'Name', render: (m) => `${m.firstName} ${m.lastName}` },
      { label: 'Phone', key: 'phoneNumber' },
      { label: 'Email', render: (m) => m.email || rawHtml('<span class="text-muted">—</span>') },
      { label: 'Status', render: (m) => rawHtml(`<span class="badge badge-${STATUS_BADGE[m.status] || 'neutral'}">${m.status}</span>`) },
      { label: 'Joined', render: (m) => new Date(m.joinedOnUtc).toLocaleDateString() },
    ],
    fetchPage: (params) => api.get('/members', params),
    rowActions: (member) => {
      const actions = [];
      if (authStore.hasPermission('members:update')) {
        actions.push({ label: 'Edit', onClick: (row, reload) => openMemberModal(row, reload) });
        actions.push(member.status === 'Frozen'
          ? { label: 'Unfreeze', onClick: async (row, reload) => { await api.post(`/members/${row.id}/unfreeze`); toastSuccess('Member unfrozen.'); reload(); } }
          : { label: 'Freeze', onClick: async (row, reload) => { await api.post(`/members/${row.id}/freeze`); toastSuccess('Member frozen.'); reload(); } });
      }
      if (authStore.hasPermission('members:delete')) {
        actions.push({
          label: 'Delete',
          className: 'btn-danger',
          onClick: async (row, reload) => {
            if (await confirmDialog(`Delete member "${row.firstName} ${row.lastName}"?`)) {
              await api.delete(`/members/${row.id}`);
              toastSuccess('Member deleted.');
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
