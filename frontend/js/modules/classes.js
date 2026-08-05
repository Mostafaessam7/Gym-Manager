import { api } from '../api/apiClient.js';
import { createDataTable } from '../components/dataTable.js';
import { openModal } from '../components/modal.js';
import { renderForm, readForm } from '../components/form.js';
import { toastSuccess, toastError } from '../components/toast.js';
import { authStore } from '../auth/authStore.js';
import { rawHtml } from '../utils/html.js';
import { t, tStatus } from '../i18n/index.js';

async function openClassModal(existing, onSaved) {
  const trainers = await api.get('/trainers');
  const fields = [
    { name: 'name', label: t('classes.className'), value: existing?.name, required: true },
    { name: 'description', label: t('classes.description'), type: 'textarea', value: existing?.description, span2: true },
    { name: 'trainerId', label: t('classes.trainer'), type: 'select', required: true, value: existing?.trainerId, options: trainers.map((tr) => ({ value: tr.id, label: `${tr.firstName} ${tr.lastName}` })) },
    { name: 'capacity', label: t('classes.capacity'), type: 'number', value: existing?.capacity ?? 10, required: true },
    { name: 'durationMinutes', label: t('classes.duration'), type: 'number', value: existing?.durationMinutes ?? 60, required: true },
  ];

  if (!existing) {
    const branches = await api.get('/branches');
    fields.splice(2, 0, { name: 'branchId', label: t('classes.branch'), type: 'select', required: true, options: branches.map((b) => ({ value: b.id, label: b.name })) });
  }

  const body = renderForm(fields);

  openModal({
    title: existing ? t('classes.editClass') : t('classes.newClassTitle'),
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
            if (existing) await api.put(`/classes/${existing.id}`, values);
            else await api.post('/classes', values);
            toastSuccess(existing ? t('classes.classUpdated') : t('classes.classCreated'));
            ctrl.close();
            onSaved();
          } catch (error) {
            toastError(error.message || t('classes.saveFailed'));
          }
        },
      },
    ],
  });
}

async function openScheduleModal(onSaved) {
  const classes = await api.get('/classes');
  const fields = [
    { name: 'gymClassId', label: t('classes.class'), type: 'select', required: true, options: classes.map((c) => ({ value: c.id, label: c.name })) },
    { name: 'startUtc', label: t('classes.start'), type: 'datetime-local', required: true },
    { name: 'endUtc', label: t('classes.end'), type: 'datetime-local', required: true },
    { name: 'capacityOverride', label: t('classes.capacityOverride'), type: 'number' },
  ];
  const body = renderForm(fields);

  openModal({
    title: t('classes.scheduleSessionTitle'),
    wide: true,
    bodyHtml: '',
    onMount: (ctrl) => ctrl.bodyElement.appendChild(body),
    footerButtons: [
      { label: t('common.cancel'), className: 'btn-secondary', onClick: (ctrl) => ctrl.close() },
      {
        label: t('classes.schedule'),
        className: 'btn-primary',
        onClick: async (ctrl) => {
          const values = readForm(body, fields);
          try {
            await api.post('/class-sessions', {
              ...values,
              startUtc: new Date(values.startUtc).toISOString(),
              endUtc: new Date(values.endUtc).toISOString(),
            });
            toastSuccess(t('classes.sessionScheduled'));
            ctrl.close();
            onSaved();
          } catch (error) {
            toastError(error.message || t('classes.scheduleFailed'));
          }
        },
      },
    ],
  });
}

async function openBookModal(session, onSaved) {
  const members = await api.get('/members', { pageSize: 100 });
  const fields = [
    { name: 'memberId', label: t('common.member'), type: 'select', required: true, options: members.items.map((m) => ({ value: m.id, label: `${m.firstName} ${m.lastName}` })) },
  ];
  const body = renderForm(fields);

  openModal({
    title: t('classes.bookSessionTitle'),
    bodyHtml: '',
    onMount: (ctrl) => ctrl.bodyElement.appendChild(body),
    footerButtons: [
      { label: t('common.cancel'), className: 'btn-secondary', onClick: (ctrl) => ctrl.close() },
      {
        label: t('classes.book'),
        className: 'btn-primary',
        onClick: async (ctrl) => {
          try {
            await api.post(`/class-sessions/${session.id}/book`, readForm(body, fields));
            toastSuccess(t('classes.bookingConfirmed'));
            ctrl.close();
            onSaved();
          } catch (error) {
            toastError(error.message || t('classes.bookFailed'));
          }
        },
      },
    ],
  });
}

function renderClassesTab(container) {
  container.innerHTML = `
    <div class="card-header">
      ${authStore.hasPermission('classes:manage') ? `<button class="btn btn-primary" id="new-class-btn">${t('classes.newClass')}</button>` : '<span></span>'}
    </div>
    <div id="classes-table"></div>
  `;

  createDataTable(document.getElementById('classes-table'), {
    searchable: false,
    columns: [
      { label: t('classes.name'), key: 'name' },
      { label: t('classes.capacityCol'), key: 'capacity' },
      { label: t('classes.durationCol'), key: 'durationMinutes' },
      { label: t('classes.statusCol'), render: (c) => rawHtml(`<span class="badge badge-${c.isActive ? 'success' : 'neutral'}">${c.isActive ? tStatus('Active') : tStatus('Inactive')}</span>`) },
    ],
    fetchPage: () => api.get('/classes', { includeInactive: true }),
    rowActions: authStore.hasPermission('classes:manage') ? (gymClass) => [
      { label: t('common.edit'), onClick: (row, reload) => openClassModal(row, reload) },
      ...(gymClass.isActive ? [{
        label: t('common.deactivate'), className: 'btn-danger',
        onClick: async (row, reload) => { await api.post(`/classes/${row.id}/deactivate`); toastSuccess(t('classes.classDeactivated')); reload(); },
      }] : []),
    ] : null,
  });

  document.getElementById('new-class-btn')?.addEventListener('click', () => openClassModal(null, () => renderClassesTab(container)));
}

function renderSessionsTab(container) {
  container.innerHTML = `
    <div class="card-header">
      ${authStore.hasPermission('classes:manage') ? `<button class="btn btn-primary" id="schedule-btn">${t('classes.scheduleSession')}</button>` : '<span></span>'}
    </div>
    <div id="sessions-table"></div>
  `;

  const table = createDataTable(document.getElementById('sessions-table'), {
    searchable: false,
    columns: [
      { label: t('classes.startCol'), render: (s) => new Date(s.startUtc).toLocaleString() },
      { label: t('classes.bookingsCol'), render: (s) => `${s.activeBookingsCount} / ${s.capacity}` },
      { label: t('classes.statusCol'), render: (s) => rawHtml(`<span class="badge badge-info">${tStatus(s.status)}</span>`) },
    ],
    fetchPage: () => api.get('/class-sessions'),
    rowActions: (session) => {
      const actions = [];
      if (authStore.hasPermission('classes:book') && session.status === 'Scheduled') {
        actions.push({ label: t('classes.book'), onClick: (row, reload) => openBookModal(row, reload) });
      }
      if (authStore.hasPermission('classes:manage') && session.status === 'Scheduled') {
        actions.push({
          label: t('classes.cancel'), className: 'btn-danger',
          onClick: async (row, reload) => { await api.post(`/class-sessions/${row.id}/cancel`); toastSuccess(t('classes.sessionCancelled')); reload(); },
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
      <h2>${t('classes.title')}</h2>
      <div class="tabs">
        <div class="tab active" data-tab="classes">${t('classes.classCatalog')}</div>
        <div class="tab" data-tab="sessions">${t('classes.sessions')}</div>
      </div>
      <div id="tab-content"></div>
    </div>
  `;

  const tabContent = document.getElementById('tab-content');
  renderClassesTab(tabContent);

  container.querySelectorAll('.tab').forEach((tabEl) => {
    tabEl.addEventListener('click', () => {
      container.querySelectorAll('.tab').forEach((el) => el.classList.remove('active'));
      tabEl.classList.add('active');
      if (tabEl.dataset.tab === 'classes') renderClassesTab(tabContent);
      else renderSessionsTab(tabContent);
    });
  });
}
