import { api } from '../api/apiClient.js';
import { createDataTable } from '../components/dataTable.js';
import { openModal } from '../components/modal.js';
import { escapeHtml } from '../utils/html.js';
import { t, tStatus } from '../i18n/index.js';

function openChangesModal(log) {
  let pretty = log.changes;
  try { pretty = JSON.stringify(JSON.parse(log.changes), null, 2); } catch { /* not JSON, show as-is */ }

  openModal({
    title: t('auditLogs.changesTitle'),
    wide: true,
    bodyHtml: `<pre style="white-space: pre-wrap; word-break: break-word;">${escapeHtml(pretty)}</pre>`,
    footerButtons: [{ label: t('common.close'), className: 'btn-secondary', onClick: (ctrl) => ctrl.close() }],
  });
}

export function render(container) {
  container.innerHTML = `
    <div class="card">
      <div class="section-title">
        <h2>${t('auditLogs.title')}</h2>
        <p class="text-muted">${t('auditLogs.subtitle')}</p>
      </div>
      <div class="form-grid" style="margin-bottom: 16px;">
        <div class="form-field">
          <label for="filter-entity-name">${t('auditLogs.filterEntityName')}</label>
          <input type="text" id="filter-entity-name" placeholder="Member, Payment, …" />
        </div>
        <div class="form-field">
          <label for="filter-entity-id">${t('auditLogs.filterEntityId')}</label>
          <input type="text" id="filter-entity-id" />
        </div>
        <div class="form-field">
          <label for="filter-user-id">${t('auditLogs.filterUserId')}</label>
          <input type="text" id="filter-user-id" />
        </div>
        <div class="form-field" style="align-self: end;">
          <button class="btn btn-primary" id="apply-filters-btn">${t('auditLogs.apply')}</button>
        </div>
      </div>
      <div id="audit-logs-table"></div>
    </div>
  `;

  const entityNameInput = document.getElementById('filter-entity-name');
  const entityIdInput = document.getElementById('filter-entity-id');
  const userIdInput = document.getElementById('filter-user-id');

  const table = createDataTable(document.getElementById('audit-logs-table'), {
    searchable: false,
    getExtraParams: () => ({
      entityName: entityNameInput.value || undefined,
      entityId: entityIdInput.value || undefined,
      userId: userIdInput.value || undefined,
    }),
    columns: [
      { label: t('auditLogs.timeCol'), render: (l) => new Date(l.timestampUtc).toLocaleString() },
      { label: t('auditLogs.entityCol'), render: (l) => `${l.entityName} (${l.entityId.slice(0, 8)}…)` },
      { label: t('auditLogs.actionCol'), render: (l) => tStatus(l.action) },
      { label: t('auditLogs.userCol'), render: (l) => l.userEmail || t('auditLogs.system') },
    ],
    fetchPage: (params) => api.get('/audit-logs', params),
    rowActions: () => [{ label: t('auditLogs.viewChanges'), onClick: (row) => openChangesModal(row) }],
  });

  document.getElementById('apply-filters-btn').addEventListener('click', () => { table.state.pageNumber = 1; table.refresh(); });
}
