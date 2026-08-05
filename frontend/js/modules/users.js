import { api } from '../api/apiClient.js';
import { createDataTable } from '../components/dataTable.js';
import { openModal } from '../components/modal.js';
import { renderForm, readForm } from '../components/form.js';
import { toastSuccess, toastError } from '../components/toast.js';
import { authStore } from '../auth/authStore.js';
import { rawHtml } from '../utils/html.js';
import { t, tStatus } from '../i18n/index.js';

async function openNewUserModal(onSaved) {
  const roles = await api.get('/roles');
  const fields = [
    { name: 'email', label: t('users.email'), type: 'email', required: true },
    { name: 'password', label: t('users.tempPassword'), type: 'password', required: true },
    { name: 'firstName', label: t('users.firstName'), required: true },
    { name: 'lastName', label: t('users.lastName'), required: true },
    { name: 'roleId', label: t('users.role'), type: 'select', options: roles.map((r) => ({ value: r.id, label: r.name })) },
  ];
  const body = renderForm(fields);

  openModal({
    title: t('users.newUserTitle'),
    wide: true,
    bodyHtml: '',
    onMount: (ctrl) => ctrl.bodyElement.appendChild(body),
    footerButtons: [
      { label: t('common.cancel'), className: 'btn-secondary', onClick: (ctrl) => ctrl.close() },
      {
        label: t('users.create'),
        className: 'btn-primary',
        onClick: async (ctrl) => {
          const values = readForm(body, fields);
          try {
            await api.post('/users', { ...values, roleIds: values.roleId ? [values.roleId] : [] });
            toastSuccess(t('users.userCreated'));
            ctrl.close();
            onSaved();
          } catch (error) {
            toastError(error.message || t('users.createUserFailed'));
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
      <div class="form-field"><label>${t('users.roleName')}</label><input type="text" id="role-name" required /></div>
      <div class="form-field span-2"><label>${t('users.description')}</label><input type="text" id="role-description" /></div>
    </div>
    <label style="font-size:0.8rem; font-weight:600; color:var(--color-text-muted);">${t('users.permissions')}</label>
    <div style="max-height:240px; overflow-y:auto; display:grid; grid-template-columns: repeat(auto-fit, minmax(180px,1fr)); gap:6px; margin-top:8px;">
      ${permissions.map((p) => `<label style="font-size:0.82rem; display:flex; gap:6px; align-items:center;"><input type="checkbox" value="${p}" class="perm-checkbox" /> ${p}</label>`).join('')}
    </div>
  `;

  openModal({
    title: t('users.newRoleTitle'),
    wide: true,
    bodyHtml: '',
    onMount: (ctrl) => ctrl.bodyElement.appendChild(body),
    footerButtons: [
      { label: t('common.cancel'), className: 'btn-secondary', onClick: (ctrl) => ctrl.close() },
      {
        label: t('users.create'),
        className: 'btn-primary',
        onClick: async (ctrl) => {
          const name = body.querySelector('#role-name').value.trim();
          const description = body.querySelector('#role-description').value.trim();
          const selectedPermissions = [...body.querySelectorAll('.perm-checkbox:checked')].map((el) => el.value);

          if (!name) { toastError(t('users.roleNameRequired')); return; }

          try {
            await api.post('/roles', { name, description, permissions: selectedPermissions });
            toastSuccess(t('users.roleCreated'));
            ctrl.close();
            onSaved();
          } catch (error) {
            toastError(error.message || t('users.createRoleFailed'));
          }
        },
      },
    ],
  });
}

function renderUsersTab(container) {
  container.innerHTML = `
    <div class="card-header">
      ${authStore.hasPermission('users:create') ? `<button class="btn btn-primary" id="new-user-btn">${t('users.newUser')}</button>` : '<span></span>'}
    </div>
    <div id="users-table"></div>
  `;

  createDataTable(document.getElementById('users-table'), {
    columns: [
      { label: t('users.emailCol'), key: 'email' },
      { label: t('users.nameCol'), render: (u) => `${u.firstName} ${u.lastName}` },
      { label: t('users.rolesCol'), render: (u) => u.roles.join(', ') || rawHtml(`<span class="text-muted">${t('users.noneCol')}</span>`) },
      { label: t('users.statusCol'), render: (u) => rawHtml(`<span class="badge badge-${u.isActive ? 'success' : 'neutral'}">${u.isActive ? tStatus('Active') : tStatus('Inactive')}</span>`) },
    ],
    fetchPage: (params) => api.get('/users', params),
    rowActions: authStore.hasPermission('users:deactivate') ? (user) => (user.isActive ? [{
      label: t('users.deactivate'), className: 'btn-danger',
      onClick: async (row, reload) => { await api.post(`/users/${row.id}/deactivate`); toastSuccess(t('users.userDeactivated')); reload(); },
    }] : []) : null,
  });

  document.getElementById('new-user-btn')?.addEventListener('click', () => openNewUserModal(() => renderUsersTab(container)));
}

function renderRolesTab(container) {
  container.innerHTML = `
    <div class="card-header">
      ${authStore.hasPermission('roles:manage') ? `<button class="btn btn-primary" id="new-role-btn">${t('users.newRole')}</button>` : '<span></span>'}
    </div>
    <div id="roles-table"></div>
  `;

  createDataTable(document.getElementById('roles-table'), {
    searchable: false,
    columns: [
      { label: t('users.roleNameCol'), key: 'name' },
      { label: t('users.descriptionCol'), key: 'description' },
      { label: t('users.permissionsCol'), render: (r) => r.permissions.length },
      { label: t('users.systemRoleCol'), render: (r) => (r.isSystemRole ? t('common.yes') : t('common.no')) },
    ],
    fetchPage: () => api.get('/roles'),
  });

  document.getElementById('new-role-btn')?.addEventListener('click', () => openNewRoleModal(() => renderRolesTab(container)));
}

export function render(container) {
  container.innerHTML = `
    <div class="card">
      <h2>${t('users.title')}</h2>
      <div class="tabs">
        <div class="tab active" data-tab="users">${t('users.tabUsers')}</div>
        <div class="tab" data-tab="roles">${t('users.tabRoles')}</div>
      </div>
      <div id="tab-content"></div>
    </div>
  `;

  const tabContent = document.getElementById('tab-content');
  renderUsersTab(tabContent);

  container.querySelectorAll('.tab').forEach((tabEl) => {
    tabEl.addEventListener('click', () => {
      container.querySelectorAll('.tab').forEach((el) => el.classList.remove('active'));
      tabEl.classList.add('active');
      if (tabEl.dataset.tab === 'users') renderUsersTab(tabContent);
      else renderRolesTab(tabContent);
    });
  });
}
