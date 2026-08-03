import { api } from '../api/apiClient.js';
import { createDataTable } from '../components/dataTable.js';
import { openModal } from '../components/modal.js';
import { renderForm, readForm } from '../components/form.js';
import { toastSuccess, toastError } from '../components/toast.js';
import { authStore } from '../auth/authStore.js';
import { rawHtml } from '../utils/html.js';

async function openClassModal(existing, onSaved) {
  const trainers = await api.get('/trainers');
  const fields = [
    { name: 'name', label: 'Class Name', value: existing?.name, required: true },
    { name: 'description', label: 'Description', type: 'textarea', value: existing?.description, span2: true },
    { name: 'trainerId', label: 'Trainer', type: 'select', required: true, value: existing?.trainerId, options: trainers.map((t) => ({ value: t.id, label: `${t.firstName} ${t.lastName}` })) },
    { name: 'capacity', label: 'Capacity', type: 'number', value: existing?.capacity ?? 10, required: true },
    { name: 'durationMinutes', label: 'Duration (minutes)', type: 'number', value: existing?.durationMinutes ?? 60, required: true },
  ];

  if (!existing) {
    const branches = await api.get('/branches');
    fields.splice(2, 0, { name: 'branchId', label: 'Branch', type: 'select', required: true, options: branches.map((b) => ({ value: b.id, label: b.name })) });
  }

  const body = renderForm(fields);

  openModal({
    title: existing ? 'Edit Class' : 'New Class',
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
          try {
            if (existing) await api.put(`/classes/${existing.id}`, values);
            else await api.post('/classes', values);
            toastSuccess(`Class ${existing ? 'updated' : 'created'}.`);
            ctrl.close();
            onSaved();
          } catch (error) {
            toastError(error.message || 'Failed to save class.');
          }
        },
      },
    ],
  });
}

async function openScheduleModal(onSaved) {
  const classes = await api.get('/classes');
  const fields = [
    { name: 'gymClassId', label: 'Class', type: 'select', required: true, options: classes.map((c) => ({ value: c.id, label: c.name })) },
    { name: 'startUtc', label: 'Start', type: 'datetime-local', required: true },
    { name: 'endUtc', label: 'End', type: 'datetime-local', required: true },
    { name: 'capacityOverride', label: 'Capacity Override', type: 'number' },
  ];
  const body = renderForm(fields);

  openModal({
    title: 'Schedule Session',
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
            await api.post('/class-sessions', {
              ...values,
              startUtc: new Date(values.startUtc).toISOString(),
              endUtc: new Date(values.endUtc).toISOString(),
            });
            toastSuccess('Session scheduled.');
            ctrl.close();
            onSaved();
          } catch (error) {
            toastError(error.message || 'Failed to schedule session.');
          }
        },
      },
    ],
  });
}

async function openBookModal(session, onSaved) {
  const members = await api.get('/members', { pageSize: 100 });
  const fields = [
    { name: 'memberId', label: 'Member', type: 'select', required: true, options: members.items.map((m) => ({ value: m.id, label: `${m.firstName} ${m.lastName}` })) },
  ];
  const body = renderForm(fields);

  openModal({
    title: 'Book Session',
    bodyHtml: '',
    onMount: (ctrl) => ctrl.bodyElement.appendChild(body),
    footerButtons: [
      { label: 'Cancel', className: 'btn-secondary', onClick: (ctrl) => ctrl.close() },
      {
        label: 'Book',
        className: 'btn-primary',
        onClick: async (ctrl) => {
          try {
            await api.post(`/class-sessions/${session.id}/book`, readForm(body, fields));
            toastSuccess('Booking confirmed.');
            ctrl.close();
            onSaved();
          } catch (error) {
            toastError(error.message || 'Failed to book session.');
          }
        },
      },
    ],
  });
}

function renderClassesTab(container) {
  container.innerHTML = `
    <div class="card-header">
      ${authStore.hasPermission('classes:manage') ? '<button class="btn btn-primary" id="new-class-btn">+ New Class</button>' : '<span></span>'}
    </div>
    <div id="classes-table"></div>
  `;

  createDataTable(document.getElementById('classes-table'), {
    searchable: false,
    columns: [
      { label: 'Name', key: 'name' },
      { label: 'Capacity', key: 'capacity' },
      { label: 'Duration (min)', key: 'durationMinutes' },
      { label: 'Status', render: (c) => rawHtml(`<span class="badge badge-${c.isActive ? 'success' : 'neutral'}">${c.isActive ? 'Active' : 'Inactive'}</span>`) },
    ],
    fetchPage: () => api.get('/classes', { includeInactive: true }),
    rowActions: authStore.hasPermission('classes:manage') ? (gymClass) => [
      { label: 'Edit', onClick: (row, reload) => openClassModal(row, reload) },
      ...(gymClass.isActive ? [{
        label: 'Deactivate', className: 'btn-danger',
        onClick: async (row, reload) => { await api.post(`/classes/${row.id}/deactivate`); toastSuccess('Class deactivated.'); reload(); },
      }] : []),
    ] : null,
  });

  document.getElementById('new-class-btn')?.addEventListener('click', () => openClassModal(null, () => renderClassesTab(container)));
}

function renderSessionsTab(container) {
  container.innerHTML = `
    <div class="card-header">
      ${authStore.hasPermission('classes:manage') ? '<button class="btn btn-primary" id="schedule-btn">+ Schedule Session</button>' : '<span></span>'}
    </div>
    <div id="sessions-table"></div>
  `;

  const table = createDataTable(document.getElementById('sessions-table'), {
    searchable: false,
    columns: [
      { label: 'Start', render: (s) => new Date(s.startUtc).toLocaleString() },
      { label: 'Bookings', render: (s) => `${s.activeBookingsCount} / ${s.capacity}` },
      { label: 'Status', render: (s) => rawHtml(`<span class="badge badge-info">${s.status}</span>`) },
    ],
    fetchPage: () => api.get('/class-sessions'),
    rowActions: (session) => {
      const actions = [];
      if (authStore.hasPermission('classes:book') && session.status === 'Scheduled') {
        actions.push({ label: 'Book', onClick: (row, reload) => openBookModal(row, reload) });
      }
      if (authStore.hasPermission('classes:manage') && session.status === 'Scheduled') {
        actions.push({
          label: 'Cancel', className: 'btn-danger',
          onClick: async (row, reload) => { await api.post(`/class-sessions/${row.id}/cancel`); toastSuccess('Session cancelled.'); reload(); },
        });
      }
      return actions;
    },
  });

  document.getElementById('schedule-btn')?.addEventListener('click', () => openScheduleModal(table.refresh));
}

export function render(container) {
  container.innerHTML = `
    <div class="card">
      <h2>Classes & Scheduling</h2>
      <div class="tabs">
        <div class="tab active" data-tab="classes">Class Catalog</div>
        <div class="tab" data-tab="sessions">Sessions</div>
      </div>
      <div id="tab-content"></div>
    </div>
  `;

  const tabContent = document.getElementById('tab-content');
  renderClassesTab(tabContent);

  container.querySelectorAll('.tab').forEach((tabEl) => {
    tabEl.addEventListener('click', () => {
      container.querySelectorAll('.tab').forEach((t) => t.classList.remove('active'));
      tabEl.classList.add('active');
      if (tabEl.dataset.tab === 'classes') renderClassesTab(tabContent);
      else renderSessionsTab(tabContent);
    });
  });
}
