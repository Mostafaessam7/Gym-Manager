import { api } from '../api/apiClient.js';
import { createDataTable } from '../components/dataTable.js';
import { openModal } from '../components/modal.js';
import { renderForm, readForm } from '../components/form.js';
import { toastSuccess, toastError } from '../components/toast.js';
import { rawHtml } from '../utils/html.js';
import { t } from '../i18n/index.js';

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

export function render(container) {
  container.innerHTML = `
    <div class="card">
      <div class="card-header">
        <h2>${t('settings.title')}</h2>
        <button class="btn btn-primary" id="new-setting-btn">${t('settings.addSetting')}</button>
      </div>
      <div id="settings-table"></div>
    </div>
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
