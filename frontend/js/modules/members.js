import { api } from '../api/apiClient.js';
import { createDataTable } from '../components/dataTable.js';
import { openModal, confirmDialog } from '../components/modal.js';
import { renderForm, readForm } from '../components/form.js';
import { toastSuccess, toastError } from '../components/toast.js';
import { authStore } from '../auth/authStore.js';
import { escapeHtml, rawHtml, safeUrl } from '../utils/html.js';
import { wireTabs } from '../components/tabs.js';
import { navigate } from '../router.js';
import { t, tStatus } from '../i18n/index.js';

const STATUS_BADGE = { Active: 'success', Frozen: 'warning', Inactive: 'neutral' };
const DOCUMENT_TYPES = () => [
  { value: 0, label: t('members.docIdCard') }, { value: 1, label: t('members.docWaiver') },
  { value: 2, label: t('members.docMedicalCertificate') }, { value: 3, label: t('members.docContract') },
  { value: 4, label: t('members.docOther') },
];

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

/* ================= List view ================= */

function renderList(container) {
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
      const actions = [{ label: t('common.view'), onClick: (row) => navigate(`/members/${row.id}`) }];
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

/* ================= Detail view ================= */

const canUpdate = () => authStore.hasPermission('members:update');

async function renderOverviewTab(container, member) {
  container.innerHTML = `
    <dl class="detail-list">
      <div><dt>${t('members.code')}</dt><dd>${escapeHtml(member.memberCode)}</dd></div>
      <div><dt>${t('common.email')}</dt><dd>${member.email ? escapeHtml(member.email) : '—'}</dd></div>
      <div><dt>${t('common.phoneNumber')}</dt><dd>${escapeHtml(member.phoneNumber)}</dd></div>
      <div><dt>${t('members.statusCol')}</dt><dd><span class="badge badge-${STATUS_BADGE[member.status] || 'neutral'}">${tStatus(member.status)}</span></dd></div>
      <div><dt>${t('members.dob')}</dt><dd>${member.dateOfBirth || '—'}</dd></div>
      <div><dt>${t('members.gender')}</dt><dd>${tStatus(member.gender) || member.gender}</dd></div>
      <div><dt>${t('members.city')}</dt><dd>${member.city ? escapeHtml(member.city) : '—'}</dd></div>
      <div><dt>${t('members.country')}</dt><dd>${member.country ? escapeHtml(member.country) : '—'}</dd></div>
      <div><dt>${t('members.emergencyContact')}</dt><dd>${member.emergencyContactName ? escapeHtml(member.emergencyContactName) : '—'}</dd></div>
      <div><dt>${t('members.emergencyPhone')}</dt><dd>${member.emergencyContactPhone ? escapeHtml(member.emergencyContactPhone) : '—'}</dd></div>
      <div><dt>${t('members.joined')}</dt><dd>${new Date(member.joinedOnUtc).toLocaleDateString()}</dd></div>
    </dl>
  `;
}

async function renderMedicalTab(container, member, onSaved) {
  const info = member.medicalInfo || {};
  const fields = [
    { name: 'bloodType', label: t('members.bloodType'), value: info.bloodType },
    { name: 'conditions', label: t('members.conditions'), type: 'textarea', value: info.conditions, span2: true },
    { name: 'allergies', label: t('members.allergies'), type: 'textarea', value: info.allergies, span2: true },
    { name: 'medications', label: t('members.medications'), type: 'textarea', value: info.medications, span2: true },
    { name: 'notes', label: t('common.notes'), type: 'textarea', value: info.notes, span2: true },
  ];
  const body = renderForm(fields);
  container.innerHTML = '';
  container.appendChild(body);

  if (canUpdate()) {
    const saveBtn = document.createElement('button');
    saveBtn.className = 'btn btn-primary';
    saveBtn.style.marginTop = '12px';
    saveBtn.textContent = t('common.save');
    saveBtn.addEventListener('click', async () => {
      try {
        await api.put(`/members/${member.id}/medical-info`, readForm(body, fields));
        toastSuccess(t('members.medicalInfoSaved'));
        onSaved();
      } catch (error) {
        toastError(error.message || t('members.medicalInfoSaveFailed'));
      }
    });
    container.appendChild(saveBtn);
  }
}

async function renderDocumentsTab(container, member, onSaved) {
  const rows = member.documents.length
    ? member.documents.map((doc) => `
      <div class="sub-list__row">
        <div><a href="${safeUrl(doc.fileUrl)}" target="_blank" rel="noopener">${escapeHtml(doc.fileName)}</a>
        <span class="sub-list__row-meta">${tStatus(doc.documentType)} · ${new Date(doc.uploadedOnUtc).toLocaleDateString()}</span></div>
        ${canUpdate() ? `<button class="btn btn-sm btn-danger" data-doc-id="${doc.id}">${t('common.delete')}</button>` : ''}
      </div>`).join('')
    : `<div class="sub-list__row text-muted">${t('members.noDocuments')}</div>`;

  container.innerHTML = `
    <div class="sub-list">${rows}</div>
    ${canUpdate() ? `
      <div class="form-grid" style="margin-top: 16px;">
        <div class="form-field">
          <label for="doc-type">${t('members.documentType')}</label>
          <select id="doc-type"></select>
        </div>
        <div class="form-field">
          <label for="doc-file">${t('members.chooseFile')}</label>
          <input type="file" id="doc-file" accept=".pdf,image/*" />
        </div>
        <div class="form-field" style="align-self: end;">
          <button class="btn btn-primary" id="upload-doc-btn">${t('members.uploadDocument')}</button>
        </div>
      </div>` : ''}
  `;

  if (canUpdate()) {
    const typeSelect = document.getElementById('doc-type');
    DOCUMENT_TYPES().forEach((opt) => {
      const el = document.createElement('option');
      el.value = opt.value;
      el.textContent = opt.label;
      typeSelect.appendChild(el);
    });

    container.querySelectorAll('[data-doc-id]').forEach((btn) => {
      btn.addEventListener('click', async () => {
        if (await confirmDialog(t('members.deleteDocumentConfirm'))) {
          await api.delete(`/members/${member.id}/documents/${btn.dataset.docId}`);
          toastSuccess(t('members.documentDeleted'));
          onSaved();
        }
      });
    });

    document.getElementById('upload-doc-btn').addEventListener('click', async () => {
      const fileInput = document.getElementById('doc-file');
      const file = fileInput.files[0];
      if (!file) { toastError(t('members.chooseFileFirst')); return; }

      try {
        const formData = new FormData();
        formData.append('file', file);
        const uploaded = await api.postForm('/files', formData);
        await api.post(`/members/${member.id}/documents`, {
          fileName: file.name, fileUrl: uploaded.url, documentType: Number(typeSelect.value),
        });
        toastSuccess(t('members.documentUploaded'));
        onSaved();
      } catch (error) {
        toastError(error.message || t('members.uploadFailed'));
      }
    });
  }
}

function measurementFields(measurement = {}) {
  return [
    { name: 'recordedOnUtc', label: t('members.recordedOn'), type: 'date', value: measurement.recordedOnUtc?.slice(0, 10), required: true },
    { name: 'heightCm', label: t('members.heightCm'), type: 'number', step: '0.1', value: measurement.heightCm },
    { name: 'weightKg', label: t('members.weightKg'), type: 'number', step: '0.1', value: measurement.weightKg },
    { name: 'bodyFatPercentage', label: t('members.bodyFatPercentage'), type: 'number', step: '0.1', value: measurement.bodyFatPercentage },
    { name: 'chestCm', label: t('members.chestCm'), type: 'number', step: '0.1', value: measurement.chestCm },
    { name: 'waistCm', label: t('members.waistCm'), type: 'number', step: '0.1', value: measurement.waistCm },
    { name: 'hipsCm', label: t('members.hipsCm'), type: 'number', step: '0.1', value: measurement.hipsCm },
    { name: 'armCm', label: t('members.armCm'), type: 'number', step: '0.1', value: measurement.armCm },
    { name: 'thighCm', label: t('members.thighCm'), type: 'number', step: '0.1', value: measurement.thighCm },
    { name: 'notes', label: t('common.notes'), type: 'textarea', value: measurement.notes, span2: true },
  ];
}

async function openMeasurementModal(memberId, existing, onSaved) {
  const fields = measurementFields(existing || {});
  const body = renderForm(fields);

  openModal({
    title: existing ? t('members.editMeasurementTitle') : t('members.recordMeasurementTitle'),
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
          values.recordedOnUtc = new Date(values.recordedOnUtc).toISOString();
          try {
            if (existing) await api.put(`/body-measurements/${existing.id}`, values);
            else await api.post('/body-measurements', { ...values, memberId });
            toastSuccess(t('members.measurementSaved'));
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

function renderMeasurementsTab(container, member) {
  container.innerHTML = `
    <div class="card-header">
      ${canUpdate() ? `<button class="btn btn-primary" id="new-measurement-btn">${t('members.recordMeasurement')}</button>` : '<span></span>'}
    </div>
    <div id="measurements-table"></div>
  `;

  const table = createDataTable(document.getElementById('measurements-table'), {
    searchable: false,
    getExtraParams: () => ({ memberId: member.id }),
    columns: [
      { label: t('members.recordedOn'), render: (m) => new Date(m.recordedOnUtc).toLocaleDateString() },
      { label: t('members.weightKg'), render: (m) => m.weightKg ?? '—' },
      { label: t('members.bmi'), render: (m) => m.bmi ?? '—' },
      { label: t('members.bodyFatPercentage'), render: (m) => (m.bodyFatPercentage != null ? `${m.bodyFatPercentage}%` : '—') },
      { label: t('members.chestCm'), render: (m) => m.chestCm ?? '—' },
      { label: t('members.waistCm'), render: (m) => m.waistCm ?? '—' },
    ],
    fetchPage: (params) => api.get('/body-measurements', params),
    rowActions: canUpdate() ? (measurement) => [
      { label: t('common.edit'), onClick: (row, reload) => openMeasurementModal(member.id, row, reload) },
      {
        label: t('common.delete'), className: 'btn-danger',
        onClick: async (row, reload) => {
          if (await confirmDialog(t('members.deleteMeasurementConfirm'))) {
            await api.delete(`/body-measurements/${row.id}`);
            toastSuccess(t('members.measurementDeleted'));
            reload();
          }
        },
      },
    ] : null,
  });

  document.getElementById('new-measurement-btn')?.addEventListener('click', () => openMeasurementModal(member.id, null, table.refresh));
}

async function renderTimelineTab(container, member) {
  container.innerHTML = '<div class="spinner"></div>';
  try {
    const entries = await api.get(`/members/${member.id}/timeline`);
    container.innerHTML = entries.length
      ? `<div class="sub-list">${entries.map((e) => `
        <div class="sub-list__row">
          <div><span class="badge badge-info">${tStatus(e.eventType)}</span>
          <span class="sub-list__row-meta">${new Date(e.occurredOnUtc).toLocaleString()}</span></div>
          <div class="sub-list__row-main">${escapeHtml(e.description)}</div>
        </div>`).join('')}</div>`
      : `<div class="empty-state">${t('members.noTimelineEntries')}</div>`;
  } catch (error) {
    container.innerHTML = `<div class="empty-state">${t('common.failedToLoadData')}</div>`;
    toastError(error.message || t('common.failedToLoadData'));
  }
}

async function renderDetail(container, memberId) {
  container.innerHTML = '<div class="spinner"></div>';

  let member;
  try {
    member = await api.get(`/members/${memberId}`);
  } catch (error) {
    container.innerHTML = `<div class="empty-state">${t('common.failedToLoadData')}</div>`;
    toastError(error.message || t('common.failedToLoadData'));
    return;
  }

  container.innerHTML = `
    <div class="card">
      <div class="card-header">
        <h2>${escapeHtml(`${member.firstName} ${member.lastName}`)} <span class="text-muted" style="font-weight: 400; font-size: 0.85rem;">(${escapeHtml(member.memberCode)})</span></h2>
        <button class="btn btn-secondary btn-sm" id="back-to-members-btn">${t('members.backToList')}</button>
      </div>
      <div class="tabs">
        <div class="tab active" data-tab="overview">${t('members.tabOverview')}</div>
        <div class="tab" data-tab="medical">${t('members.tabMedical')}</div>
        <div class="tab" data-tab="documents">${t('members.tabDocuments')}</div>
        <div class="tab" data-tab="measurements">${t('members.tabMeasurements')}</div>
        <div class="tab" data-tab="timeline">${t('members.tabTimeline')}</div>
      </div>
      <div id="detail-tab-content"><div class="spinner"></div></div>
    </div>
  `;

  document.getElementById('back-to-members-btn').addEventListener('click', () => navigate('/members'));

  const tabContent = document.getElementById('detail-tab-content');
  const renderers = {
    overview: () => renderOverviewTab(tabContent, member),
    medical: () => renderMedicalTab(tabContent, member, refreshMemberAndCurrentTab),
    documents: () => renderDocumentsTab(tabContent, member, refreshMemberAndCurrentTab),
    measurements: () => renderMeasurementsTab(tabContent, member),
    timeline: () => renderTimelineTab(tabContent, member),
  };

  // Re-fetches just the member (patching the existing object in place so every renderer closure above still
  // sees the update) and re-renders only the currently-active tab, instead of rebuilding the whole detail
  // page — a full renderDetail() re-run was both an extra round-trip per save/upload and silently bounced
  // the user back to the Overview tab after every medical-info save or document upload/delete.
  async function refreshMemberAndCurrentTab() {
    try {
      Object.assign(member, await api.get(`/members/${memberId}`));
    } catch (error) {
      toastError(error.message || t('common.failedToLoadData'));
    }
    const activeTab = container.querySelector('.tab.active')?.dataset.tab || 'overview';
    tabContent.innerHTML = '<div class="spinner"></div>';
    renderers[activeTab]();
  }

  renderers.overview();

  wireTabs(container, tabContent, renderers);
}

/* ================= Entry point ================= */

export function render(container, fullPath = '/members') {
  const segments = fullPath.split('/').filter(Boolean);
  const memberId = segments.length > 1 ? segments[1] : null;

  if (memberId) renderDetail(container, memberId);
  else renderList(container);
}
