import { api } from '../api/apiClient.js';
import { createDataTable } from '../components/dataTable.js';
import { openModal, confirmDialog } from '../components/modal.js';
import { renderForm, readForm } from '../components/form.js';
import { toastSuccess, toastError } from '../components/toast.js';
import { authStore } from '../auth/authStore.js';
import { rawHtml } from '../utils/html.js';
import { t, tStatus } from '../i18n/index.js';

const canManage = () => authStore.hasPermission('staff:manage');

const LEAVE_TYPES = () => [
  { value: 0, label: t('staff.leaveVacation') }, { value: 1, label: t('staff.leaveSick') },
  { value: 2, label: t('staff.leaveUnpaid') }, { value: 3, label: t('staff.leaveOther') },
];
const COMMISSION_SOURCES = () => [
  { value: 0, label: t('staff.sourceClassSession') }, { value: 1, label: t('staff.sourcePersonalTraining') },
  { value: 2, label: t('staff.sourceProductSale') }, { value: 3, label: t('staff.sourceOther') },
];
const SHIFT_BADGE = { Scheduled: 'info', Completed: 'success', Cancelled: 'neutral', NoShow: 'danger' };
const LEAVE_BADGE = { Pending: 'warning', Approved: 'success', Rejected: 'danger' };
const COMMISSION_BADGE = { Pending: 'warning', Paid: 'success' };

// Not every role that can see this page (e.g. Trainer, who only has staff:view) also has users:view, so the
// staff directory lookup is best-effort — a caller without it just sees themselves as the only pickable
// option and raw ids (shortened) for anyone else's records instead of a broken page.
let userOptionsCache = null;
async function userOptions() {
  if (userOptionsCache) return userOptionsCache;
  try {
    const users = await api.get('/users', { pageSize: 200 });
    userOptionsCache = (users.items || users).map((u) => ({ value: u.id, label: `${u.firstName} ${u.lastName}` }));
  } catch {
    const session = authStore.session;
    userOptionsCache = session ? [{ value: session.userId, label: `${session.email} (${t('common.you')})` }] : [];
  }
  return userOptionsCache;
}

function userName(users, id) {
  const known = users.find((u) => u.value === id);
  if (known) return known.label;
  if (id === authStore.session?.userId) return t('common.you');
  return id ? `${id.slice(0, 8)}…` : '—';
}

/* ---------------- Shifts ---------------- */

async function openScheduleShiftModal(onSaved) {
  const [users, branches] = await Promise.all([userOptions(), api.get('/branches')]);
  const fields = [
    { name: 'userId', label: t('staff.staffMember'), type: 'select', required: true, options: users },
    { name: 'branchId', label: t('staff.branch'), type: 'select', required: true, options: branches.map((b) => ({ value: b.id, label: b.name })) },
    { name: 'startUtc', label: t('staff.start'), type: 'datetime-local', required: true },
    { name: 'endUtc', label: t('staff.end'), type: 'datetime-local', required: true },
    { name: 'notes', label: t('staff.notes'), type: 'textarea', span2: true },
  ];
  const body = renderForm(fields);

  openModal({
    title: t('staff.scheduleShiftTitle'),
    wide: true,
    bodyHtml: '',
    onMount: (ctrl) => ctrl.bodyElement.appendChild(body),
    footerButtons: [
      { label: t('common.cancel'), className: 'btn-secondary', onClick: (ctrl) => ctrl.close() },
      {
        label: t('staff.schedule'),
        className: 'btn-primary',
        onClick: async (ctrl) => {
          const values = readForm(body, fields);
          try {
            await api.post('/staff-shifts', { ...values, startUtc: new Date(values.startUtc).toISOString(), endUtc: new Date(values.endUtc).toISOString() });
            toastSuccess(t('staff.shiftScheduled'));
            ctrl.close();
            onSaved();
          } catch (error) {
            toastError(error.message || t('staff.saveFailed'));
          }
        },
      },
    ],
  });
}

async function renderShiftsTab(container) {
  const users = await userOptions();
  container.innerHTML = `
    <div class="card-header">
      ${canManage() ? `<button class="btn btn-primary" id="new-shift-btn">${t('staff.scheduleShift')}</button>` : '<span></span>'}
    </div>
    <div id="shifts-table"></div>
  `;

  createDataTable(document.getElementById('shifts-table'), {
    searchable: false,
    columns: [
      { label: t('staff.staffCol'), render: (s) => userName(users, s.userId) },
      { label: t('staff.startCol'), render: (s) => new Date(s.startUtc).toLocaleString() },
      { label: t('staff.endCol'), render: (s) => new Date(s.endUtc).toLocaleString() },
      { label: t('staff.statusCol'), render: (s) => rawHtml(`<span class="badge badge-${SHIFT_BADGE[s.status] || 'neutral'}">${tStatus(s.status)}</span>`) },
      { label: t('staff.notesCol'), render: (s) => s.notes || rawHtml('<span class="text-muted">—</span>') },
    ],
    fetchPage: (params) => api.get('/staff-shifts', params),
    rowActions: canManage() ? (shift) => (shift.status === 'Scheduled' ? [
      { label: t('staff.complete'), onClick: async (row, reload) => { await api.post(`/staff-shifts/${row.id}/complete`); toastSuccess(t('staff.shiftCompleted')); reload(); } },
      { label: t('staff.noShow'), className: 'btn-secondary', onClick: async (row, reload) => { await api.post(`/staff-shifts/${row.id}/no-show`); toastSuccess(t('staff.shiftNoShow')); reload(); } },
      { label: t('staff.cancel'), className: 'btn-danger', onClick: async (row, reload) => {
        if (await confirmDialog(t('staff.cancelShiftConfirm'))) { await api.post(`/staff-shifts/${row.id}/cancel`); toastSuccess(t('staff.shiftCancelled')); reload(); }
      } },
    ] : []) : null,
  });

  document.getElementById('new-shift-btn')?.addEventListener('click', () => openScheduleShiftModal(() => renderShiftsTab(container)));
}

/* ---------------- Leave Requests ---------------- */

async function openRequestLeaveModal(onSaved) {
  const users = await userOptions();
  const fields = [
    { name: 'userId', label: t('staff.staffMember'), type: 'select', required: true, options: users },
    { name: 'type', label: t('staff.type'), type: 'select', options: LEAVE_TYPES() },
    { name: 'startDate', label: t('staff.startDate'), type: 'date', required: true },
    { name: 'endDate', label: t('staff.endDate'), type: 'date', required: true },
    { name: 'reason', label: t('staff.reason'), type: 'textarea', span2: true },
  ];
  const body = renderForm(fields);

  openModal({
    title: t('staff.requestLeaveTitle'),
    wide: true,
    bodyHtml: '',
    onMount: (ctrl) => ctrl.bodyElement.appendChild(body),
    footerButtons: [
      { label: t('common.cancel'), className: 'btn-secondary', onClick: (ctrl) => ctrl.close() },
      {
        label: t('staff.submit'),
        className: 'btn-primary',
        onClick: async (ctrl) => {
          const values = readForm(body, fields);
          try {
            await api.post('/leave-requests', values);
            toastSuccess(t('staff.leaveSubmitted'));
            ctrl.close();
            onSaved();
          } catch (error) {
            toastError(error.message || t('staff.submitFailed'));
          }
        },
      },
    ],
  });
}

async function renderLeaveTab(container) {
  const users = await userOptions();
  container.innerHTML = `
    <div class="card-header">
      <button class="btn btn-primary" id="new-leave-btn">${t('staff.requestLeave')}</button>
    </div>
    <div id="leave-table"></div>
  `;

  createDataTable(document.getElementById('leave-table'), {
    searchable: false,
    columns: [
      { label: t('staff.staffCol'), render: (l) => userName(users, l.userId) },
      { label: t('staff.typeCol'), render: (l) => tStatus(l.type) },
      { label: t('staff.fromCol'), render: (l) => l.startDate },
      { label: t('staff.toCol'), render: (l) => l.endDate },
      { label: t('staff.statusCol'), render: (l) => rawHtml(`<span class="badge badge-${LEAVE_BADGE[l.status] || 'neutral'}">${tStatus(l.status)}</span>`) },
      { label: t('staff.reasonCol'), render: (l) => l.reason || rawHtml('<span class="text-muted">—</span>') },
    ],
    fetchPage: (params) => api.get('/leave-requests', params),
    rowActions: canManage() ? (leave) => (leave.status === 'Pending' ? [
      { label: t('staff.approve'), onClick: async (row, reload) => { await api.post(`/leave-requests/${row.id}/approve`, { notes: null }); toastSuccess(t('staff.leaveApproved')); reload(); } },
      { label: t('staff.reject'), className: 'btn-danger', onClick: async (row, reload) => {
        const notes = window.prompt(t('staff.rejectReasonPrompt')) || null;
        await api.post(`/leave-requests/${row.id}/reject`, { notes });
        toastSuccess(t('staff.leaveRejected'));
        reload();
      } },
    ] : []) : null,
  });

  document.getElementById('new-leave-btn')?.addEventListener('click', () => openRequestLeaveModal(() => renderLeaveTab(container)));
}

/* ---------------- Commissions ---------------- */

async function openRecordCommissionModal(onSaved) {
  const users = await userOptions();
  const fields = [
    { name: 'userId', label: t('staff.staffMember'), type: 'select', required: true, options: users },
    { name: 'amount', label: t('staff.amount'), type: 'number', step: '0.01', required: true },
    { name: 'sourceType', label: t('staff.source'), type: 'select', options: COMMISSION_SOURCES() },
    { name: 'earnedOnUtc', label: t('staff.earnedOn'), type: 'date', required: true },
    { name: 'notes', label: t('staff.notes'), type: 'textarea', span2: true },
  ];
  const body = renderForm(fields);

  openModal({
    title: t('staff.recordCommissionTitle'),
    wide: true,
    bodyHtml: '',
    onMount: (ctrl) => ctrl.bodyElement.appendChild(body),
    footerButtons: [
      { label: t('common.cancel'), className: 'btn-secondary', onClick: (ctrl) => ctrl.close() },
      {
        label: t('staff.record'),
        className: 'btn-primary',
        onClick: async (ctrl) => {
          const values = readForm(body, fields);
          values.earnedOnUtc = new Date(values.earnedOnUtc).toISOString();
          try {
            await api.post('/commissions', values);
            toastSuccess(t('staff.commissionRecorded'));
            ctrl.close();
            onSaved();
          } catch (error) {
            toastError(error.message || t('staff.recordFailed'));
          }
        },
      },
    ],
  });
}

async function renderCommissionsTab(container) {
  const users = await userOptions();
  container.innerHTML = `
    <div class="card-header">
      ${canManage() ? `<button class="btn btn-primary" id="new-commission-btn">${t('staff.recordCommission')}</button>` : '<span></span>'}
    </div>
    <div id="commissions-table"></div>
  `;

  createDataTable(document.getElementById('commissions-table'), {
    searchable: false,
    columns: [
      { label: t('staff.staffCol'), render: (c) => userName(users, c.userId) },
      { label: t('staff.amountCol'), render: (c) => `${c.amount} ${c.currency}` },
      { label: t('staff.sourceCol'), render: (c) => tStatus(c.sourceType) },
      { label: t('staff.earnedCol'), render: (c) => new Date(c.earnedOnUtc).toLocaleDateString() },
      { label: t('staff.statusCol'), render: (c) => rawHtml(`<span class="badge badge-${COMMISSION_BADGE[c.status] || 'neutral'}">${tStatus(c.status)}</span>`) },
    ],
    fetchPage: (params) => api.get('/commissions', params),
    rowActions: canManage() ? (commission) => (commission.status === 'Pending' ? [
      { label: t('staff.markPaid'), onClick: async (row, reload) => { await api.post(`/commissions/${row.id}/mark-paid`); toastSuccess(t('staff.commissionMarkedPaid')); reload(); } },
    ] : []) : null,
  });

  document.getElementById('new-commission-btn')?.addEventListener('click', () => openRecordCommissionModal(() => renderCommissionsTab(container)));
}

export function render(container) {
  container.innerHTML = `
    <div class="card">
      <div class="section-title">
        <h2>${t('staff.title')}</h2>
        <p class="text-muted">${t('staff.subtitle')}</p>
      </div>
      <div class="tabs">
        <div class="tab active" data-tab="shifts">${t('staff.tabShifts')}</div>
        <div class="tab" data-tab="leave">${t('staff.tabLeave')}</div>
        <div class="tab" data-tab="commissions">${t('staff.tabCommissions')}</div>
      </div>
      <div id="tab-content"><div class="spinner"></div></div>
    </div>
  `;

  const tabContent = document.getElementById('tab-content');
  renderShiftsTab(tabContent);

  container.querySelectorAll('.tab').forEach((tabEl) => {
    tabEl.addEventListener('click', () => {
      container.querySelectorAll('.tab').forEach((el) => el.classList.remove('active'));
      tabEl.classList.add('active');
      tabContent.innerHTML = '<div class="spinner"></div>';
      if (tabEl.dataset.tab === 'shifts') renderShiftsTab(tabContent);
      else if (tabEl.dataset.tab === 'leave') renderLeaveTab(tabContent);
      else renderCommissionsTab(tabContent);
    });
  });
}
