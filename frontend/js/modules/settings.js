import { api } from '../api/apiClient.js';
import { createDataTable } from '../components/dataTable.js';
import { openModal, confirmDialog } from '../components/modal.js';
import { renderForm, readForm } from '../components/form.js';
import { toastSuccess, toastError } from '../components/toast.js';
import { authStore } from '../auth/authStore.js';
import { escapeHtml, rawHtml } from '../utils/html.js';
import { wireTabs } from '../components/tabs.js';
import { t } from '../i18n/index.js';

/* ================= Global Settings tab (unchanged behavior) ================= */

function openSettingModal(onSaved) {
  const fields = [
    { name: 'key', label: t('settings.key'), required: true },
    { name: 'value', label: t('settings.value'), required: true },
    { name: 'description', label: t('settings.description'), type: 'textarea', span2: true },
  ];
  const body = renderForm(fields);

  openModal({
    title: t('settings.addSettingTitle'),
    wide: true,
    bodyHtml: '',
    onMount: (ctrl) => ctrl.bodyElement.appendChild(body),
    footerButtons: [
      { label: t('common.cancel'), className: 'btn-secondary', onClick: (ctrl) => ctrl.close() },
      {
        label: t('settings.save'),
        className: 'btn-primary',
        onClick: async (ctrl) => {
          try {
            await api.put('/settings', readForm(body, fields));
            toastSuccess(t('settings.settingSaved'));
            ctrl.close();
            onSaved();
          } catch (error) {
            toastError(error.message || t('settings.saveFailed'));
          }
        },
      },
    ],
  });
}

function renderGlobalSettingsTab(container) {
  container.innerHTML = `
    <div class="card-header">
      <span></span>
      <button class="btn btn-primary" id="new-setting-btn">${t('settings.addSetting')}</button>
    </div>
    <div id="settings-table"></div>
  `;

  const table = createDataTable(document.getElementById('settings-table'), {
    searchable: false,
    columns: [
      { label: t('settings.keyCol'), key: 'key' },
      { label: t('settings.valueCol'), key: 'value' },
      { label: t('settings.descriptionCol'), render: (s) => s.description || rawHtml('<span class="text-muted">—</span>') },
    ],
    fetchPage: () => api.get('/settings'),
    rowActions: (setting) => [
      {
        label: t('settings.delete'), className: 'btn-danger',
        onClick: async (row, reload) => { await api.delete(`/settings/${row.id}`); toastSuccess(t('settings.settingDeleted')); reload(); },
      },
    ],
  });

  document.getElementById('new-setting-btn').addEventListener('click', () => openSettingModal(table.refresh));
}

/* ================= My Account tab: change password ================= */

function renderChangePasswordSection(container) {
  const fields = [
    { name: 'currentPassword', label: t('settings.currentPassword'), type: 'password', required: true },
    { name: 'newPassword', label: t('settings.newPassword'), type: 'password', required: true },
    { name: 'confirmPassword', label: t('settings.confirmPassword'), type: 'password', required: true },
  ];
  const body = renderForm(fields);
  container.innerHTML = '';
  container.appendChild(body);

  const saveBtn = document.createElement('button');
  saveBtn.className = 'btn btn-primary';
  saveBtn.style.marginTop = '12px';
  saveBtn.textContent = t('settings.changePassword');
  saveBtn.addEventListener('click', async () => {
    const values = readForm(body, fields);
    if (values.newPassword !== values.confirmPassword) {
      toastError(t('settings.passwordsDontMatch'));
      return;
    }
    try {
      await api.post('/auth/change-password', { currentPassword: values.currentPassword, newPassword: values.newPassword });
      toastSuccess(t('settings.passwordChanged'));
      fields.forEach((f) => { const el = body.querySelector(`#field-${f.name}`); if (el) el.value = ''; });
    } catch (error) {
      toastError(error.message || t('settings.changePasswordFailed'));
    }
  });
  container.appendChild(saveBtn);
}

/* ================= My Account tab: two-factor authentication ================= */

async function openTwoFactorSetupModal(onChanged) {
  let setup;
  try {
    setup = await api.post('/auth/2fa/setup');
  } catch (error) {
    toastError(error.message || t('settings.twoFactorSetupFailed'));
    return;
  }

  const body = document.createElement('div');
  body.innerHTML = `
    <p>${t('settings.twoFactorSetupInstructions')}</p>
    <dl class="detail-list">
      <div><dt>${t('settings.secretKey')}</dt><dd><code>${escapeHtml(setup.secretKey)}</code></dd></div>
      <div><dt>${t('settings.provisioningUri')}</dt><dd style="word-break: break-all;"><code>${escapeHtml(setup.provisioningUri)}</code></dd></div>
    </dl>
  `;
  const codeWrap = document.createElement('div');
  codeWrap.className = 'form-grid';
  const codeField = { name: 'code', label: t('settings.enterCodeToConfirm'), required: true };
  codeWrap.appendChild(renderForm([codeField]));
  body.appendChild(codeWrap);

  openModal({
    title: t('settings.setUpTwoFactorTitle'),
    wide: true,
    bodyHtml: '',
    onMount: (ctrl) => ctrl.bodyElement.appendChild(body),
    footerButtons: [
      { label: t('common.cancel'), className: 'btn-secondary', onClick: (ctrl) => ctrl.close() },
      {
        label: t('common.confirm'),
        className: 'btn-primary',
        onClick: async (ctrl) => {
          const code = codeWrap.querySelector(`#field-${codeField.name}`).value;
          try {
            const result = await api.post('/auth/2fa/confirm', { code });
            ctrl.close();
            showRecoveryCodesModal(result.recoveryCodes);
            onChanged();
          } catch (error) {
            toastError(error.message || t('settings.twoFactorConfirmFailed'));
          }
        },
      },
    ],
  });
}

function showRecoveryCodesModal(codes) {
  openModal({
    title: t('settings.recoveryCodesTitle'),
    wide: true,
    bodyHtml: `
      <p>${t('settings.recoveryCodesWarning')}</p>
      <pre style="white-space: pre-wrap;">${(codes || []).map(escapeHtml).join('\n')}</pre>
    `,
    footerButtons: [{ label: t('common.close'), className: 'btn-primary', onClick: (ctrl) => ctrl.close() }],
  });
}

function openDisableTwoFactorModal(onChanged) {
  const field = { name: 'currentPassword', label: t('settings.currentPassword'), type: 'password', required: true };
  const body = renderForm([field]);

  openModal({
    title: t('settings.disableTwoFactorTitle'),
    wide: true,
    bodyHtml: '',
    onMount: (ctrl) => ctrl.bodyElement.appendChild(body),
    footerButtons: [
      { label: t('common.cancel'), className: 'btn-secondary', onClick: (ctrl) => ctrl.close() },
      {
        label: t('common.confirm'),
        className: 'btn-danger',
        onClick: async (ctrl) => {
          const currentPassword = body.querySelector(`#field-${field.name}`).value;
          try {
            await api.post('/auth/2fa/disable', { currentPassword });
            toastSuccess(t('settings.twoFactorDisabled'));
            ctrl.close();
            onChanged();
          } catch (error) {
            toastError(error.message || t('settings.twoFactorDisableFailed'));
          }
        },
      },
    ],
  });
}

function renderTwoFactorSection(container) {
  container.innerHTML = `
    <p class="text-muted">${t('settings.twoFactorDescription')}</p>
    <div style="display: flex; gap: 8px; margin-top: 12px;">
      <button class="btn btn-primary" id="setup-2fa-btn">${t('settings.setUpTwoFactor')}</button>
      <button class="btn btn-danger" id="disable-2fa-btn">${t('settings.disableTwoFactor')}</button>
    </div>
  `;

  document.getElementById('setup-2fa-btn').addEventListener('click', () => openTwoFactorSetupModal(() => {}));
  document.getElementById('disable-2fa-btn').addEventListener('click', () => openDisableTwoFactorModal(() => {}));
}

/* ================= My Account tab: sessions ================= */

async function renderSessionsSection(container) {
  container.innerHTML = '<div class="spinner"></div>';

  async function load() {
    let sessions;
    try {
      sessions = await api.get('/auth/sessions');
    } catch (error) {
      container.innerHTML = `<div class="empty-state">${t('common.failedToLoadData')}</div>`;
      toastError(error.message || t('common.failedToLoadData'));
      return;
    }

    container.innerHTML = `
      <div class="card-header">
        <span></span>
        <button class="btn btn-danger" id="revoke-all-btn">${t('settings.revokeAllSessions')}</button>
      </div>
      <div class="sub-list">
        ${sessions.length ? sessions.map((s) => `
          <div class="sub-list__row">
            <div><span class="badge badge-${s.isActive ? 'success' : 'neutral'}">${s.isActive ? t('settings.sessionActive') : t('settings.sessionInactive')}</span>
            <span class="sub-list__row-meta">${escapeHtml(s.ipAddress || '—')} · ${escapeHtml(s.userAgent || '—')}</span></div>
            <div class="sub-list__row-main">${t('settings.sessionCreated')}: ${new Date(s.createdOnUtc).toLocaleString()}</div>
            ${s.isActive ? `<button class="btn btn-sm btn-danger" data-session-id="${s.id}">${t('settings.revoke')}</button>` : ''}
          </div>`).join('') : `<div class="sub-list__row text-muted">${t('settings.noSessions')}</div>`}
      </div>
    `;

    container.querySelectorAll('[data-session-id]').forEach((btn) => {
      btn.addEventListener('click', async () => {
        if (await confirmDialog(t('settings.revokeSessionConfirm'))) {
          await api.delete(`/auth/sessions/${btn.dataset.sessionId}`);
          toastSuccess(t('settings.sessionRevoked'));
          load();
        }
      });
    });

    document.getElementById('revoke-all-btn').addEventListener('click', async () => {
      if (await confirmDialog(t('settings.revokeAllConfirm'))) {
        await api.post('/auth/sessions/revoke-all');
        toastSuccess(t('settings.allSessionsRevoked'));
        authStore.clear();
        window.location.href = 'index.html';
      }
    });
  }

  load();
}

function renderMyAccountTab(container) {
  container.innerHTML = `
    <div style="display: grid; gap: 24px;">
      <div>
        <h3>${t('settings.changePasswordTitle')}</h3>
        <div id="account-password"></div>
      </div>
      <div>
        <h3>${t('settings.twoFactorTitle')}</h3>
        <div id="account-2fa"></div>
      </div>
      <div>
        <h3>${t('settings.sessionsTitle')}</h3>
        <div id="account-sessions"></div>
      </div>
    </div>
  `;
  renderChangePasswordSection(document.getElementById('account-password'));
  renderTwoFactorSection(document.getElementById('account-2fa'));
  renderSessionsSection(document.getElementById('account-sessions'));
}

/* ================= Entry point ================= */

export function render(container) {
  container.innerHTML = `
    <div class="card">
      <div class="section-title">
        <h2>${t('settings.pageTitle')}</h2>
      </div>
      <div class="tabs">
        <div class="tab active" data-tab="global">${t('settings.tabGlobal')}</div>
        <div class="tab" data-tab="account">${t('settings.tabAccount')}</div>
      </div>
      <div id="settings-tab-content"><div class="spinner"></div></div>
    </div>
  `;

  const tabContent = document.getElementById('settings-tab-content');
  const renderers = {
    global: () => renderGlobalSettingsTab(tabContent),
    account: () => renderMyAccountTab(tabContent),
  };
  renderers.global();

  wireTabs(container, tabContent, renderers);
}
