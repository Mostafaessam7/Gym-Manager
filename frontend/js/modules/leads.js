import { api } from '../api/apiClient.js';
import { openModal } from '../components/modal.js';
import { renderForm, readForm } from '../components/form.js';
import { toastSuccess, toastError } from '../components/toast.js';
import { authStore } from '../auth/authStore.js';
import { escapeHtml } from '../utils/html.js';
import { t, tStatus } from '../i18n/index.js';

const SOURCES = () => [
  { value: 0, label: t('leads.sourceWebsite'), name: 'Website' }, { value: 1, label: t('leads.sourceReferral'), name: 'Referral' },
  { value: 2, label: t('leads.sourceWalkIn'), name: 'WalkIn' }, { value: 3, label: t('leads.sourceSocialMedia'), name: 'SocialMedia' },
  { value: 4, label: t('leads.sourcePhoneCall'), name: 'PhoneCall' }, { value: 5, label: t('leads.sourceOther'), name: 'Other' },
];
const STAGES = ['New', 'Contacted', 'Qualified', 'ProposalSent', 'Won', 'Lost'];
const STAGE_LABEL = () => ({
  New: t('leads.stageNew'), Contacted: t('leads.stageContacted'), Qualified: t('leads.stageQualified'),
  ProposalSent: t('leads.stageProposalSent'), Won: t('leads.stageWon'), Lost: t('leads.stageLost'),
});
const FOLLOW_UP_TYPES = () => [
  { value: 0, label: t('leads.typeCall') }, { value: 1, label: t('leads.typeEmail') },
  { value: 2, label: t('leads.typeMeeting') }, { value: 3, label: t('leads.typeOther') },
];
const GENDERS = () => [{ value: 0, label: t('common.unspecified') }, { value: 1, label: t('common.male') }, { value: 2, label: t('common.female') }];

const canManage = () => authStore.hasPermission('crm:manage');

function leadFields(lead = {}) {
  return [
    { name: 'name', label: t('leads.name'), value: lead.name, required: true },
    { name: 'email', label: t('leads.email'), type: 'email', value: lead.email },
    { name: 'phone', label: t('leads.phone'), value: lead.phone },
    { name: 'source', label: t('leads.source'), type: 'select', value: SOURCES().find((s) => s.name === lead.source)?.value ?? 0, options: SOURCES() },
    { name: 'notes', label: t('leads.notes'), type: 'textarea', value: lead.notes, span2: true },
  ];
}

async function openLeadModal(existing, onSaved) {
  const fields = leadFields(existing || {});
  const body = renderForm(fields);

  openModal({
    title: existing ? t('leads.editLeadTitle') : t('leads.newLeadTitle'),
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
            if (existing) await api.put(`/leads/${existing.id}`, values);
            else await api.post('/leads', values);
            toastSuccess(existing ? t('leads.leadUpdated') : t('leads.leadCreated'));
            ctrl.close();
            onSaved();
          } catch (error) {
            toastError(error.message || t('leads.saveFailed'));
          }
        },
      },
    ],
  });
}

function renderFollowUps(lead) {
  if (!lead.followUps?.length) return `<p class="text-muted">${t('leads.noFollowUps')}</p>`;
  return `<ul class="timeline">${lead.followUps
    .slice()
    .sort((a, b) => new Date(b.scheduledOnUtc) - new Date(a.scheduledOnUtc))
    .map((f) => `
      <li class="timeline-item${f.isCompleted ? ' is-done' : ''}">
        <div class="timeline-item__title">${escapeHtml(tStatus(f.type))} ${f.isCompleted ? `<span class="badge badge-success">${t('leads.done')}</span>` : ''}</div>
        <div class="timeline-item__meta">${new Date(f.scheduledOnUtc).toLocaleString()}${f.completedOnUtc ? ` · ${new Date(f.completedOnUtc).toLocaleString()}` : ''}</div>
        ${f.notes ? `<div class="timeline-item__notes">${escapeHtml(f.notes)}</div>` : ''}
        ${!f.isCompleted && canManage() ? `<button class="btn btn-sm btn-secondary complete-followup" data-id="${f.id}" style="margin-top:6px;">${t('leads.markComplete')}</button>` : ''}
      </li>`).join('')}</ul>`;
}

async function openLeadDetailModal(leadSummary, onChanged) {
  const lead = await api.get(`/leads/${leadSummary.id}`);
  const stageLabels = STAGE_LABEL();

  const body = document.createElement('div');
  body.innerHTML = `
    <dl class="detail-list">
      <div><dt>${t('leads.detailStage')}</dt><dd><span class="badge badge-info">${stageLabels[lead.stage] || lead.stage}</span></dd></div>
      <div><dt>${t('leads.detailSource')}</dt><dd>${escapeHtml(tStatus(lead.source))}</dd></div>
      <div><dt>${t('leads.detailEmail')}</dt><dd>${escapeHtml(lead.email || '—')}</dd></div>
      <div><dt>${t('leads.detailPhone')}</dt><dd>${escapeHtml(lead.phone || '—')}</dd></div>
      <div><dt>${t('leads.detailCreated')}</dt><dd>${new Date(lead.createdOnUtc).toLocaleDateString()}</dd></div>
      ${lead.lostReason ? `<div><dt>${t('leads.detailLostReason')}</dt><dd>${escapeHtml(lead.lostReason)}</dd></div>` : ''}
    </dl>
    ${lead.notes ? `<p>${escapeHtml(lead.notes)}</p>` : ''}
    <h4 style="margin-top: var(--spacing-4);">${t('leads.followUps')}</h4>
    <div id="followups-list">${renderFollowUps(lead)}</div>
    ${canManage() && lead.stage !== 'Won' && lead.stage !== 'Lost' ? `
      <h4 style="margin-top: var(--spacing-4);">${t('leads.scheduleFollowUp')}</h4>
      <div class="form-grid" id="followup-form">
        <div class="form-field"><label>${t('leads.followUpType')}</label><select id="fu-type">${FOLLOW_UP_TYPES().map((ft) => `<option value="${ft.value}">${ft.label}</option>`).join('')}</select></div>
        <div class="form-field"><label>${t('leads.when')}</label><input type="datetime-local" id="fu-when" /></div>
        <div class="form-field span-2"><label>${t('leads.notes')}</label><input type="text" id="fu-notes" /></div>
      </div>
      <button class="btn btn-sm btn-secondary" id="fu-add-btn" style="margin-top:8px;">${t('leads.addFollowUp')}</button>
    ` : ''}
  `;

  const footerButtons = [{ label: t('leads.close'), className: 'btn-secondary', onClick: (ctrl) => ctrl.close() }];

  if (canManage() && lead.stage !== 'Won' && lead.stage !== 'Lost') {
    footerButtons.unshift(
      { label: t('leads.assignToMe'), className: 'btn-secondary', onClick: async () => {
        try { await api.post(`/leads/${lead.id}/assign`, { userId: authStore.session.userId }); toastSuccess(t('leads.leadAssigned')); onChanged(); } catch (e) { toastError(e.message); }
      } },
      { label: t('leads.markLost'), className: 'btn-danger', onClick: async (ctrl) => {
        const reason = window.prompt(t('leads.reasonPrompt')) || null;
        try { await api.post(`/leads/${lead.id}/mark-lost`, { reason }); toastSuccess(t('leads.leadMarkedLost')); ctrl.close(); onChanged(); } catch (e) { toastError(e.message); }
      } },
      { label: t('leads.convertToMember'), className: 'btn-primary', onClick: (ctrl) => { ctrl.close(); openConvertModal(lead, onChanged); } },
    );
  }
  if (canManage() && lead.stage === 'Lost') {
    footerButtons.unshift({ label: t('leads.reopen'), className: 'btn-secondary', onClick: async (ctrl) => {
      try { await api.post(`/leads/${lead.id}/reopen`); toastSuccess(t('leads.leadReopened')); ctrl.close(); onChanged(); } catch (e) { toastError(e.message); }
    } });
  }

  openModal({
    title: lead.name,
    wide: true,
    bodyHtml: '',
    onMount: (ctrl) => {
      ctrl.bodyElement.appendChild(body);
      body.querySelector('#fu-add-btn')?.addEventListener('click', async () => {
        const type = Number(body.querySelector('#fu-type').value);
        const when = body.querySelector('#fu-when').value;
        const notes = body.querySelector('#fu-notes').value || null;
        if (!when) { toastError(t('leads.pickDateTime')); return; }
        try {
          await api.post(`/leads/${lead.id}/follow-ups`, { type, scheduledOnUtc: new Date(when).toISOString(), notes });
          toastSuccess(t('leads.followUpScheduled'));
          ctrl.close();
          openLeadDetailModal(leadSummary, onChanged);
        } catch (error) {
          toastError(error.message || t('leads.scheduleFollowUpFailed'));
        }
      });
      body.querySelectorAll('.complete-followup').forEach((btn) => {
        btn.addEventListener('click', async () => {
          try {
            await api.post(`/leads/${lead.id}/follow-ups/${btn.dataset.id}/complete`, { completedOnUtc: new Date().toISOString(), notes: null });
            toastSuccess(t('leads.followUpCompleted'));
            ctrl.close();
            openLeadDetailModal(leadSummary, onChanged);
          } catch (error) {
            toastError(error.message || t('leads.completeFollowUpFailed'));
          }
        });
      });
    },
    footerButtons,
  });
}

async function openConvertModal(lead, onChanged) {
  const branches = await api.get('/branches');
  const [firstName, ...rest] = lead.name.split(' ');
  const fields = [
    { name: 'branchId', label: t('leads.branch'), type: 'select', required: true, options: branches.map((b) => ({ value: b.id, label: b.name })) },
    { name: 'firstName', label: t('leads.firstName'), value: firstName, required: true },
    { name: 'lastName', label: t('leads.lastName'), value: rest.join(' '), required: true },
    { name: 'phoneNumber', label: t('leads.phoneNumber'), value: lead.phone, required: true },
    { name: 'email', label: t('leads.email'), type: 'email', value: lead.email },
    { name: 'dateOfBirth', label: t('leads.dob'), type: 'date' },
    { name: 'gender', label: t('leads.gender'), type: 'select', value: 0, options: GENDERS() },
  ];
  const body = renderForm(fields);

  openModal({
    title: t('leads.convertTitle', { name: lead.name }),
    wide: true,
    bodyHtml: '',
    onMount: (ctrl) => ctrl.bodyElement.appendChild(body),
    footerButtons: [
      { label: t('common.cancel'), className: 'btn-secondary', onClick: (ctrl) => ctrl.close() },
      {
        label: t('leads.convert'),
        className: 'btn-primary',
        onClick: async (ctrl) => {
          const values = readForm(body, fields);
          try {
            await api.post(`/leads/${lead.id}/convert`, values);
            toastSuccess(t('leads.leadConverted'));
            ctrl.close();
            onChanged();
          } catch (error) {
            toastError(error.message || t('leads.convertFailed'));
          }
        },
      },
    ],
  });
}

async function loadBoard(container) {
  container.innerHTML = '<div class="spinner"></div>';
  try {
    const results = await Promise.all(STAGES.map((stage, idx) => api.get('/leads', { pageSize: 50, stage: idx })));
    const columns = STAGES.map((stage, idx) => ({ stage, items: results[idx].items || results[idx] }));
    const stageLabels = STAGE_LABEL();

    container.innerHTML = `<div class="kanban">${columns.map((col) => `
      <div class="kanban-col" data-stage="${col.stage}">
        <div class="kanban-col__header"><span>${stageLabels[col.stage]}</span><span class="kanban-col__count">${col.items.length}</span></div>
        <div class="kanban-col__body">
          ${col.items.map((lead) => `
            <div class="kanban-card" data-id="${lead.id}">
              <div class="kanban-card__title">${escapeHtml(lead.name)}</div>
              <div class="kanban-card__meta">${escapeHtml(tStatus(lead.source))}${lead.phone ? ` · ${escapeHtml(lead.phone)}` : ''}</div>
            </div>`).join('') || `<p class="text-muted" style="font-size:0.78rem;">${t('leads.empty')}</p>`}
        </div>
      </div>`).join('')}</div>`;

    container.querySelectorAll('.kanban-card').forEach((card) => {
      card.addEventListener('click', () => openLeadDetailModal({ id: card.dataset.id }, () => loadBoard(container)));
    });
  } catch (error) {
    container.innerHTML = `<div class="empty-state">${t('leads.failedToLoadPipeline')}</div>`;
    toastError(error.message || t('leads.failedToLoadLeads'));
  }
}

export function render(container) {
  container.innerHTML = `
    <div class="card">
      <div class="card-header">
        <div class="section-title">
          <h2>${t('leads.title')}</h2>
          <p class="text-muted">${t('leads.subtitle')}</p>
        </div>
        ${canManage() ? `<button class="btn btn-primary" id="new-lead-btn">${t('leads.newLead')}</button>` : ''}
      </div>
      <div id="board"></div>
    </div>
  `;

  const board = document.getElementById('board');
  loadBoard(board);

  document.getElementById('new-lead-btn')?.addEventListener('click', () => openLeadModal(null, () => loadBoard(board)));
}
