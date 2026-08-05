import { api } from '../api/apiClient.js';
import { openModal, confirmDialog } from '../components/modal.js';
import { renderForm, readForm } from '../components/form.js';
import { toastSuccess, toastError } from '../components/toast.js';
import { authStore } from '../auth/authStore.js';
import { escapeHtml } from '../utils/html.js';

const SOURCES = [
  { value: 0, label: 'Website', name: 'Website' }, { value: 1, label: 'Referral', name: 'Referral' },
  { value: 2, label: 'Walk-in', name: 'WalkIn' }, { value: 3, label: 'Social Media', name: 'SocialMedia' },
  { value: 4, label: 'Phone Call', name: 'PhoneCall' }, { value: 5, label: 'Other', name: 'Other' },
];
const STAGES = ['New', 'Contacted', 'Qualified', 'ProposalSent', 'Won', 'Lost'];
const STAGE_LABEL = { New: 'New', Contacted: 'Contacted', Qualified: 'Qualified', ProposalSent: 'Proposal Sent', Won: 'Won', Lost: 'Lost' };
const FOLLOW_UP_TYPES = [{ value: 0, label: 'Call' }, { value: 1, label: 'Email' }, { value: 2, label: 'Meeting' }, { value: 3, label: 'Other' }];
const GENDERS = [{ value: 0, label: 'Unspecified' }, { value: 1, label: 'Male' }, { value: 2, label: 'Female' }];

const canManage = () => authStore.hasPermission('crm:manage');

function leadFields(lead = {}) {
  return [
    { name: 'name', label: 'Name', value: lead.name, required: true },
    { name: 'email', label: 'Email', type: 'email', value: lead.email },
    { name: 'phone', label: 'Phone', value: lead.phone },
    { name: 'source', label: 'Source', type: 'select', value: SOURCES.find((s) => s.name === lead.source)?.value ?? 0, options: SOURCES },
    { name: 'notes', label: 'Notes', type: 'textarea', value: lead.notes, span2: true },
  ];
}

async function openLeadModal(existing, onSaved) {
  const fields = leadFields(existing || {});
  const body = renderForm(fields);

  openModal({
    title: existing ? 'Edit Lead' : 'New Lead',
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
          values.source = Number(values.source);
          try {
            if (existing) await api.put(`/leads/${existing.id}`, values);
            else await api.post('/leads', values);
            toastSuccess(existing ? 'Lead updated.' : 'Lead created.');
            ctrl.close();
            onSaved();
          } catch (error) {
            toastError(error.message || 'Failed to save lead.');
          }
        },
      },
    ],
  });
}

function renderFollowUps(lead) {
  if (!lead.followUps?.length) return '<p class="text-muted">No follow-ups scheduled.</p>';
  return `<ul class="timeline">${lead.followUps
    .slice()
    .sort((a, b) => new Date(b.scheduledOnUtc) - new Date(a.scheduledOnUtc))
    .map((f) => `
      <li class="timeline-item${f.isCompleted ? ' is-done' : ''}">
        <div class="timeline-item__title">${escapeHtml(f.type)} ${f.isCompleted ? '<span class="badge badge-success">Done</span>' : ''}</div>
        <div class="timeline-item__meta">Scheduled ${new Date(f.scheduledOnUtc).toLocaleString()}${f.completedOnUtc ? ` · Completed ${new Date(f.completedOnUtc).toLocaleString()}` : ''}</div>
        ${f.notes ? `<div class="timeline-item__notes">${escapeHtml(f.notes)}</div>` : ''}
        ${!f.isCompleted && canManage() ? `<button class="btn btn-sm btn-secondary complete-followup" data-id="${f.id}" style="margin-top:6px;">Mark Complete</button>` : ''}
      </li>`).join('')}</ul>`;
}

async function openLeadDetailModal(leadSummary, onChanged) {
  const lead = await api.get(`/leads/${leadSummary.id}`);

  const body = document.createElement('div');
  body.innerHTML = `
    <dl class="detail-list">
      <div><dt>Stage</dt><dd><span class="badge badge-info">${STAGE_LABEL[lead.stage] || lead.stage}</span></dd></div>
      <div><dt>Source</dt><dd>${escapeHtml(lead.source)}</dd></div>
      <div><dt>Email</dt><dd>${escapeHtml(lead.email || '—')}</dd></div>
      <div><dt>Phone</dt><dd>${escapeHtml(lead.phone || '—')}</dd></div>
      <div><dt>Created</dt><dd>${new Date(lead.createdOnUtc).toLocaleDateString()}</dd></div>
      ${lead.lostReason ? `<div><dt>Lost Reason</dt><dd>${escapeHtml(lead.lostReason)}</dd></div>` : ''}
    </dl>
    ${lead.notes ? `<p>${escapeHtml(lead.notes)}</p>` : ''}
    <h4 style="margin-top: var(--spacing-4);">Follow-ups</h4>
    <div id="followups-list">${renderFollowUps(lead)}</div>
    ${canManage() && lead.stage !== 'Won' && lead.stage !== 'Lost' ? `
      <h4 style="margin-top: var(--spacing-4);">Schedule Follow-up</h4>
      <div class="form-grid" id="followup-form">
        <div class="form-field"><label>Type</label><select id="fu-type">${FOLLOW_UP_TYPES.map((t) => `<option value="${t.value}">${t.label}</option>`).join('')}</select></div>
        <div class="form-field"><label>When</label><input type="datetime-local" id="fu-when" /></div>
        <div class="form-field span-2"><label>Notes</label><input type="text" id="fu-notes" /></div>
      </div>
      <button class="btn btn-sm btn-secondary" id="fu-add-btn" style="margin-top:8px;">+ Add Follow-up</button>
    ` : ''}
  `;

  const footerButtons = [{ label: 'Close', className: 'btn-secondary', onClick: (ctrl) => ctrl.close() }];

  if (canManage() && lead.stage !== 'Won' && lead.stage !== 'Lost') {
    footerButtons.unshift(
      { label: 'Assign to me', className: 'btn-secondary', onClick: async () => {
        try { await api.post(`/leads/${lead.id}/assign`, { userId: authStore.session.userId }); toastSuccess('Lead assigned.'); onChanged(); } catch (e) { toastError(e.message); }
      } },
      { label: 'Mark Lost', className: 'btn-danger', onClick: async (ctrl) => {
        const reason = window.prompt('Reason (optional):') || null;
        try { await api.post(`/leads/${lead.id}/mark-lost`, { reason }); toastSuccess('Lead marked lost.'); ctrl.close(); onChanged(); } catch (e) { toastError(e.message); }
      } },
      { label: 'Convert to Member', className: 'btn-primary', onClick: (ctrl) => { ctrl.close(); openConvertModal(lead, onChanged); } },
    );
  }
  if (canManage() && lead.stage === 'Lost') {
    footerButtons.unshift({ label: 'Reopen', className: 'btn-secondary', onClick: async (ctrl) => {
      try { await api.post(`/leads/${lead.id}/reopen`); toastSuccess('Lead reopened.'); ctrl.close(); onChanged(); } catch (e) { toastError(e.message); }
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
        if (!when) { toastError('Pick a date/time for the follow-up.'); return; }
        try {
          await api.post(`/leads/${lead.id}/follow-ups`, { type, scheduledOnUtc: new Date(when).toISOString(), notes });
          toastSuccess('Follow-up scheduled.');
          ctrl.close();
          openLeadDetailModal(leadSummary, onChanged);
        } catch (error) {
          toastError(error.message || 'Failed to schedule follow-up.');
        }
      });
      body.querySelectorAll('.complete-followup').forEach((btn) => {
        btn.addEventListener('click', async () => {
          try {
            await api.post(`/leads/${lead.id}/follow-ups/${btn.dataset.id}/complete`, { completedOnUtc: new Date().toISOString(), notes: null });
            toastSuccess('Follow-up completed.');
            ctrl.close();
            openLeadDetailModal(leadSummary, onChanged);
          } catch (error) {
            toastError(error.message || 'Failed to complete follow-up.');
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
    { name: 'branchId', label: 'Branch', type: 'select', required: true, options: branches.map((b) => ({ value: b.id, label: b.name })) },
    { name: 'firstName', label: 'First Name', value: firstName, required: true },
    { name: 'lastName', label: 'Last Name', value: rest.join(' '), required: true },
    { name: 'phoneNumber', label: 'Phone Number', value: lead.phone, required: true },
    { name: 'email', label: 'Email', type: 'email', value: lead.email },
    { name: 'dateOfBirth', label: 'Date of Birth', type: 'date' },
    { name: 'gender', label: 'Gender', type: 'select', value: 0, options: GENDERS },
  ];
  const body = renderForm(fields);

  openModal({
    title: `Convert "${lead.name}" to Member`,
    wide: true,
    bodyHtml: '',
    onMount: (ctrl) => ctrl.bodyElement.appendChild(body),
    footerButtons: [
      { label: 'Cancel', className: 'btn-secondary', onClick: (ctrl) => ctrl.close() },
      {
        label: 'Convert',
        className: 'btn-primary',
        onClick: async (ctrl) => {
          const values = readForm(body, fields);
          values.gender = Number(values.gender);
          try {
            await api.post(`/leads/${lead.id}/convert`, values);
            toastSuccess('Lead converted to member.');
            ctrl.close();
            onChanged();
          } catch (error) {
            toastError(error.message || 'Failed to convert lead.');
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

    container.innerHTML = `<div class="kanban">${columns.map((col) => `
      <div class="kanban-col" data-stage="${col.stage}">
        <div class="kanban-col__header"><span>${STAGE_LABEL[col.stage]}</span><span class="kanban-col__count">${col.items.length}</span></div>
        <div class="kanban-col__body">
          ${col.items.map((lead) => `
            <div class="kanban-card" data-id="${lead.id}">
              <div class="kanban-card__title">${escapeHtml(lead.name)}</div>
              <div class="kanban-card__meta">${escapeHtml(lead.source)}${lead.phone ? ` · ${escapeHtml(lead.phone)}` : ''}</div>
            </div>`).join('') || '<p class="text-muted" style="font-size:0.78rem;">Empty</p>'}
        </div>
      </div>`).join('')}</div>`;

    container.querySelectorAll('.kanban-card').forEach((card) => {
      card.addEventListener('click', () => openLeadDetailModal({ id: card.dataset.id }, () => loadBoard(container)));
    });
  } catch (error) {
    container.innerHTML = '<div class="empty-state">Failed to load pipeline.</div>';
    toastError(error.message || 'Failed to load leads.');
  }
}

export function render(container) {
  container.innerHTML = `
    <div class="card">
      <div class="card-header">
        <div class="section-title">
          <h2>CRM Pipeline</h2>
          <p class="text-muted">Track prospective members from first contact through conversion.</p>
        </div>
        ${canManage() ? '<button class="btn btn-primary" id="new-lead-btn">+ New Lead</button>' : ''}
      </div>
      <div id="board"></div>
    </div>
  `;

  const board = document.getElementById('board');
  loadBoard(board);

  document.getElementById('new-lead-btn')?.addEventListener('click', () => openLeadModal(null, () => loadBoard(board)));
}
