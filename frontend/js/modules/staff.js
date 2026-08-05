import { api } from '../api/apiClient.js';
import { createDataTable } from '../components/dataTable.js';
import { openModal, confirmDialog } from '../components/modal.js';
import { renderForm, readForm } from '../components/form.js';
import { toastSuccess, toastError } from '../components/toast.js';
import { authStore } from '../auth/authStore.js';
import { rawHtml } from '../utils/html.js';

const canManage = () => authStore.hasPermission('staff:manage');

const LEAVE_TYPES = [{ value: 0, label: 'Vacation' }, { value: 1, label: 'Sick' }, { value: 2, label: 'Unpaid' }, { value: 3, label: 'Other' }];
const COMMISSION_SOURCES = [{ value: 0, label: 'Class Session' }, { value: 1, label: 'Personal Training' }, { value: 2, label: 'Product Sale' }, { value: 3, label: 'Other' }];
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
    userOptionsCache = session ? [{ value: session.userId, label: `${session.email} (you)` }] : [];
  }
  return userOptionsCache;
}

function userName(users, id) {
  const known = users.find((u) => u.value === id);
  if (known) return known.label;
  if (id === authStore.session?.userId) return 'You';
  return id ? `${id.slice(0, 8)}…` : '—';
}

/* ---------------- Shifts ---------------- */

async function openScheduleShiftModal(onSaved) {
  const [users, branches] = await Promise.all([userOptions(), api.get('/branches')]);
  const fields = [
    { name: 'userId', label: 'Staff Member', type: 'select', required: true, options: users },
    { name: 'branchId', label: 'Branch', type: 'select', required: true, options: branches.map((b) => ({ value: b.id, label: b.name })) },
    { name: 'startUtc', label: 'Start', type: 'datetime-local', required: true },
    { name: 'endUtc', label: 'End', type: 'datetime-local', required: true },
    { name: 'notes', label: 'Notes', type: 'textarea', span2: true },
  ];
  const body = renderForm(fields);

  openModal({
    title: 'Schedule Shift',
    wide: true,
    bodyHtml: '',
    onMount: (ctrl) => ctrl.bodyElement.appendChild(body),
    footerButtons: [
      { label: 'Cancel', className: 'btn-secondary', onClick: (ctrl) => ctrl.close() },
      {
        label: 'Schedule',
        className: 'btn-primary',
        onClick: async (ctrl) => {
          const values = readForm(body, fields);
          try {
            await api.post('/staff-shifts', { ...values, startUtc: new Date(values.startUtc).toISOString(), endUtc: new Date(values.endUtc).toISOString() });
            toastSuccess('Shift scheduled.');
            ctrl.close();
            onSaved();
          } catch (error) {
            toastError(error.message || 'Failed to schedule shift.');
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
      ${canManage() ? '<button class="btn btn-primary" id="new-shift-btn">+ Schedule Shift</button>' : '<span></span>'}
    </div>
    <div id="shifts-table"></div>
  `;

  createDataTable(document.getElementById('shifts-table'), {
    searchable: false,
    columns: [
      { label: 'Staff', render: (s) => userName(users, s.userId) },
      { label: 'Start', render: (s) => new Date(s.startUtc).toLocaleString() },
      { label: 'End', render: (s) => new Date(s.endUtc).toLocaleString() },
      { label: 'Status', render: (s) => rawHtml(`<span class="badge badge-${SHIFT_BADGE[s.status] || 'neutral'}">${s.status}</span>`) },
      { label: 'Notes', render: (s) => s.notes || rawHtml('<span class="text-muted">—</span>') },
    ],
    fetchPage: (params) => api.get('/staff-shifts', params),
    rowActions: canManage() ? (shift) => (shift.status === 'Scheduled' ? [
      { label: 'Complete', onClick: async (row, reload) => { await api.post(`/staff-shifts/${row.id}/complete`); toastSuccess('Shift marked complete.'); reload(); } },
      { label: 'No-show', className: 'btn-secondary', onClick: async (row, reload) => { await api.post(`/staff-shifts/${row.id}/no-show`); toastSuccess('Shift marked no-show.'); reload(); } },
      { label: 'Cancel', className: 'btn-danger', onClick: async (row, reload) => {
        if (await confirmDialog('Cancel this shift?')) { await api.post(`/staff-shifts/${row.id}/cancel`); toastSuccess('Shift cancelled.'); reload(); }
      } },
    ] : []) : null,
  });

  document.getElementById('new-shift-btn')?.addEventListener('click', () => openScheduleShiftModal(() => renderShiftsTab(container)));
}

/* ---------------- Leave Requests ---------------- */

async function openRequestLeaveModal(onSaved) {
  const users = await userOptions();
  const fields = [
    { name: 'userId', label: 'Staff Member', type: 'select', required: true, options: users },
    { name: 'type', label: 'Type', type: 'select', options: LEAVE_TYPES },
    { name: 'startDate', label: 'Start Date', type: 'date', required: true },
    { name: 'endDate', label: 'End Date', type: 'date', required: true },
    { name: 'reason', label: 'Reason', type: 'textarea', span2: true },
  ];
  const body = renderForm(fields);

  openModal({
    title: 'Request Leave',
    wide: true,
    bodyHtml: '',
    onMount: (ctrl) => ctrl.bodyElement.appendChild(body),
    footerButtons: [
      { label: 'Cancel', className: 'btn-secondary', onClick: (ctrl) => ctrl.close() },
      {
        label: 'Submit',
        className: 'btn-primary',
        onClick: async (ctrl) => {
          const values = readForm(body, fields);
          values.type = Number(values.type);
          try {
            await api.post('/leave-requests', values);
            toastSuccess('Leave request submitted.');
            ctrl.close();
            onSaved();
          } catch (error) {
            toastError(error.message || 'Failed to submit leave request.');
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
      <button class="btn btn-primary" id="new-leave-btn">+ Request Leave</button>
    </div>
    <div id="leave-table"></div>
  `;

  createDataTable(document.getElementById('leave-table'), {
    searchable: false,
    columns: [
      { label: 'Staff', render: (l) => userName(users, l.userId) },
      { label: 'Type', key: 'type' },
      { label: 'From', render: (l) => l.startDate },
      { label: 'To', render: (l) => l.endDate },
      { label: 'Status', render: (l) => rawHtml(`<span class="badge badge-${LEAVE_BADGE[l.status] || 'neutral'}">${l.status}</span>`) },
      { label: 'Reason', render: (l) => l.reason || rawHtml('<span class="text-muted">—</span>') },
    ],
    fetchPage: (params) => api.get('/leave-requests', params),
    rowActions: canManage() ? (leave) => (leave.status === 'Pending' ? [
      { label: 'Approve', onClick: async (row, reload) => { await api.post(`/leave-requests/${row.id}/approve`, { notes: null }); toastSuccess('Leave approved.'); reload(); } },
      { label: 'Reject', className: 'btn-danger', onClick: async (row, reload) => {
        const notes = window.prompt('Reason for rejection (optional):') || null;
        await api.post(`/leave-requests/${row.id}/reject`, { notes });
        toastSuccess('Leave rejected.');
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
    { name: 'userId', label: 'Staff Member', type: 'select', required: true, options: users },
    { name: 'amount', label: 'Amount', type: 'number', step: '0.01', required: true },
    { name: 'sourceType', label: 'Source', type: 'select', options: COMMISSION_SOURCES },
    { name: 'earnedOnUtc', label: 'Earned On', type: 'date', required: true },
    { name: 'notes', label: 'Notes', type: 'textarea', span2: true },
  ];
  const body = renderForm(fields);

  openModal({
    title: 'Record Commission',
    wide: true,
    bodyHtml: '',
    onMount: (ctrl) => ctrl.bodyElement.appendChild(body),
    footerButtons: [
      { label: 'Cancel', className: 'btn-secondary', onClick: (ctrl) => ctrl.close() },
      {
        label: 'Record',
        className: 'btn-primary',
        onClick: async (ctrl) => {
          const values = readForm(body, fields);
          values.sourceType = Number(values.sourceType);
          values.earnedOnUtc = new Date(values.earnedOnUtc).toISOString();
          try {
            await api.post('/commissions', values);
            toastSuccess('Commission recorded.');
            ctrl.close();
            onSaved();
          } catch (error) {
            toastError(error.message || 'Failed to record commission.');
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
      ${canManage() ? '<button class="btn btn-primary" id="new-commission-btn">+ Record Commission</button>' : '<span></span>'}
    </div>
    <div id="commissions-table"></div>
  `;

  createDataTable(document.getElementById('commissions-table'), {
    searchable: false,
    columns: [
      { label: 'Staff', render: (c) => userName(users, c.userId) },
      { label: 'Amount', render: (c) => `${c.amount} ${c.currency}` },
      { label: 'Source', key: 'sourceType' },
      { label: 'Earned', render: (c) => new Date(c.earnedOnUtc).toLocaleDateString() },
      { label: 'Status', render: (c) => rawHtml(`<span class="badge badge-${COMMISSION_BADGE[c.status] || 'neutral'}">${c.status}</span>`) },
    ],
    fetchPage: (params) => api.get('/commissions', params),
    rowActions: canManage() ? (commission) => (commission.status === 'Pending' ? [
      { label: 'Mark Paid', onClick: async (row, reload) => { await api.post(`/commissions/${row.id}/mark-paid`); toastSuccess('Commission marked paid.'); reload(); } },
    ] : []) : null,
  });

  document.getElementById('new-commission-btn')?.addEventListener('click', () => openRecordCommissionModal(() => renderCommissionsTab(container)));
}

export function render(container) {
  container.innerHTML = `
    <div class="card">
      <div class="section-title">
        <h2>Staff</h2>
        <p class="text-muted">Shifts, leave requests, and commissions for every staff account.</p>
      </div>
      <div class="tabs">
        <div class="tab active" data-tab="shifts">Shifts</div>
        <div class="tab" data-tab="leave">Leave Requests</div>
        <div class="tab" data-tab="commissions">Commissions</div>
      </div>
      <div id="tab-content"><div class="spinner"></div></div>
    </div>
  `;

  const tabContent = document.getElementById('tab-content');
  renderShiftsTab(tabContent);

  container.querySelectorAll('.tab').forEach((tabEl) => {
    tabEl.addEventListener('click', () => {
      container.querySelectorAll('.tab').forEach((t) => t.classList.remove('active'));
      tabEl.classList.add('active');
      tabContent.innerHTML = '<div class="spinner"></div>';
      if (tabEl.dataset.tab === 'shifts') renderShiftsTab(tabContent);
      else if (tabEl.dataset.tab === 'leave') renderLeaveTab(tabContent);
      else renderCommissionsTab(tabContent);
    });
  });
}
