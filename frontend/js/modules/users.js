import { api } from '../api/apiClient.js';
import { createDataTable } from '../components/dataTable.js';
import { openModal } from '../components/modal.js';
import { renderForm, readForm } from '../components/form.js';
import { toastSuccess, toastError } from '../components/toast.js';
import { authStore } from '../auth/authStore.js';
import { rawHtml } from '../utils/html.js';

async function openNewUserModal(onSaved) {
  const roles = await api.get('/roles');
  const fields = [
    { name: 'email', label: 'Email', type: 'email', required: true },
    { name: 'password', label: 'Temporary Password', type: 'password', required: true },
    { name: 'firstName', label: 'First Name', required: true },
    { name: 'lastName', label: 'Last Name', required: true },
    { name: 'roleId', label: 'Role', type: 'select', options: roles.map((r) => ({ value: r.id, label: r.name })) },
  ];
  const body = renderForm(fields);

  openModal({
    title: 'New Staff User',
    wide: true,
    bodyHtml: '',
    onMount: (ctrl) => ctrl.bodyElement.appendChild(body),
    footerButtons: [
      { label: 'Cancel', className: 'btn-secondary', onClick: (ctrl) => ctrl.close() },
      {
        label: 'Create',
        className: 'btn-primary',
        onClick: async (ctrl) => {
          const values = readForm(body, fields);
          try {
            await api.post('/users', { ...values, roleIds: values.roleId ? [values.roleId] : [] });
            toastSuccess('User created.');
            ctrl.close();
            onSaved();
          } catch (error) {
            toastError(error.message || 'Failed to create user.');
          }
        },
      },
    ],
  });
}

async function openNewRoleModal(onSaved) {
  const permissions = await api.get('/roles/permissions');
  const body = document.createElement('div');
  body.innerHTML = `
    <div class="form-grid" style="margin-bottom: var(--spacing-4);">
      <div class="form-field"><label>Role Name</label><input type="text" id="role-name" required /></div>
      <div class="form-field span-2"><label>Description</label><input type="text" id="role-description" /></div>
    </div>
    <label style="font-size:0.8rem; font-weight:600; color:var(--color-text-muted);">Permissions</label>
    <div style="max-height:240px; overflow-y:auto; display:grid; grid-template-columns: repeat(auto-fit, minmax(180px,1fr)); gap:6px; margin-top:8px;">
      ${permissions.map((p) => `<label style="font-size:0.82rem; display:flex; gap:6px; align-items:center;"><input type="checkbox" value="${p}" class="perm-checkbox" /> ${p}</label>`).join('')}
    </div>
  `;

  openModal({
    title: 'New Role',
    wide: true,
    bodyHtml: '',
    onMount: (ctrl) => ctrl.bodyElement.appendChild(body),
    footerButtons: [
      { label: 'Cancel', className: 'btn-secondary', onClick: (ctrl) => ctrl.close() },
      {
        label: 'Create',
        className: 'btn-primary',
        onClick: async (ctrl) => {
          const name = body.querySelector('#role-name').value.trim();
          const description = body.querySelector('#role-description').value.trim();
          const selectedPermissions = [...body.querySelectorAll('.perm-checkbox:checked')].map((el) => el.value);

          if (!name) { toastError('Role name is required.'); return; }

          try {
            await api.post('/roles', { name, description, permissions: selectedPermissions });
            toastSuccess('Role created.');
            ctrl.close();
            onSaved();
          } catch (error) {
            toastError(error.message || 'Failed to create role.');
          }
        },
      },
    ],
  });
}

function renderUsersTab(container) {
  container.innerHTML = `
    <div class="card-header">
      ${authStore.hasPermission('users:create') ? '<button class="btn btn-primary" id="new-user-btn">+ New User</button>' : '<span></span>'}
    </div>
    <div id="users-table"></div>
  `;

  createDataTable(document.getElementById('users-table'), {
    columns: [
      { label: 'Email', key: 'email' },
      { label: 'Name', render: (u) => `${u.firstName} ${u.lastName}` },
      { label: 'Roles', render: (u) => u.roles.join(', ') || rawHtml('<span class="text-muted">None</span>') },
      { label: 'Status', render: (u) => rawHtml(`<span class="badge badge-${u.isActive ? 'success' : 'neutral'}">${u.isActive ? 'Active' : 'Inactive'}</span>`) },
    ],
    fetchPage: (params) => api.get('/users', params),
    rowActions: authStore.hasPermission('users:deactivate') ? (user) => (user.isActive ? [{
      label: 'Deactivate', className: 'btn-danger',
      onClick: async (row, reload) => { await api.post(`/users/${row.id}/deactivate`); toastSuccess('User deactivated.'); reload(); },
    }] : []) : null,
  });

  document.getElementById('new-user-btn')?.addEventListener('click', () => openNewUserModal(() => renderUsersTab(container)));
}

function renderRolesTab(container) {
  container.innerHTML = `
    <div class="card-header">
      ${authStore.hasPermission('roles:manage') ? '<button class="btn btn-primary" id="new-role-btn">+ New Role</button>' : '<span></span>'}
    </div>
    <div id="roles-table"></div>
  `;

  createDataTable(document.getElementById('roles-table'), {
    searchable: false,
    columns: [
      { label: 'Name', key: 'name' },
      { label: 'Description', key: 'description' },
      { label: 'Permissions', render: (r) => r.permissions.length },
      { label: 'System Role', render: (r) => (r.isSystemRole ? 'Yes' : 'No') },
    ],
    fetchPage: () => api.get('/roles'),
  });

  document.getElementById('new-role-btn')?.addEventListener('click', () => openNewRoleModal(() => renderRolesTab(container)));
}

export function render(container) {
  container.innerHTML = `
    <div class="card">
      <h2>Users & Roles</h2>
      <div class="tabs">
        <div class="tab active" data-tab="users">Users</div>
        <div class="tab" data-tab="roles">Roles</div>
      </div>
      <div id="tab-content"></div>
    </div>
  `;

  const tabContent = document.getElementById('tab-content');
  renderUsersTab(tabContent);

  container.querySelectorAll('.tab').forEach((tabEl) => {
    tabEl.addEventListener('click', () => {
      container.querySelectorAll('.tab').forEach((t) => t.classList.remove('active'));
      tabEl.classList.add('active');
      if (tabEl.dataset.tab === 'users') renderUsersTab(tabContent);
      else renderRolesTab(tabContent);
    });
  });
}
