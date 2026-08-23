import { api } from '../api/apiClient.js';
import { createDataTable } from '../components/dataTable.js';
import { openModal } from '../components/modal.js';
import { renderForm, readForm } from '../components/form.js';
import { toastSuccess, toastError } from '../components/toast.js';
import { rawHtml } from '../utils/html.js';
import { t, tStatus } from '../i18n/index.js';

const STATUS_BADGE = { Sent: 'success', Pending: 'warning', Failed: 'danger' };

const CHANNELS = () => [
  { value: 0, label: t('notifications.channelEmail') },
  { value: 1, label: t('notifications.channelSms') },
  { value: 2, label: t('notifications.channelInApp') },
];

function openSendModal(onSent) {
  const fields = [
    { name: 'channel', label: t('notifications.channel'), type: 'select', value: 0, options: CHANNELS() },
    { name: 'recipientAddress', label: t('notifications.recipientAddress'), required: true, span2: true },
    { name: 'subject', label: t('notifications.subject'), required: true, span2: true },
    { name: 'body', label: t('notifications.body'), type: 'textarea', required: true, span2: true },
  ];
  const body = renderForm(fields);

  openModal({
    title: t('notifications.sendNotificationTitle'),
    wide: true,
    bodyHtml: '',
    onMount: (ctrl) => ctrl.bodyElement.appendChild(body),
    footerButtons: [
      { label: t('common.cancel'), className: 'btn-secondary', onClick: (ctrl) => ctrl.close() },
      {
        label: t('notifications.send'),
        className: 'btn-primary',
        onClick: async (ctrl) => {
          const values = readForm(body, fields);
          try {
            await api.post('/notifications', { ...values, recipientUserId: null, recipientMemberId: null });
            toastSuccess(t('notifications.notificationSent'));
            ctrl.close();
            onSent();
          } catch (error) {
            toastError(error.message || t('notifications.sendFailed'));
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
        <div class="section-title">
          <h2>${t('notifications.title')}</h2>
          <p class="text-muted">${t('notifications.subtitle')}</p>
        </div>
        <button class="btn btn-primary" id="send-notification-btn">${t('notifications.sendNotification')}</button>
      </div>
      <div id="notifications-table"></div>
    </div>
  `;

  const table = createDataTable(document.getElementById('notifications-table'), {
    searchable: false,
    columns: [
      { label: t('notifications.dateCol'), render: (n) => new Date(n.createdOnUtc).toLocaleString() },
      { label: t('notifications.channelCol'), render: (n) => tStatus(n.channel) },
      { label: t('notifications.recipientCol'), render: (n) => n.recipientAddress || rawHtml('<span class="text-muted">—</span>') },
      { label: t('notifications.subjectCol'), key: 'subject' },
      { label: t('notifications.statusCol'), render: (n) => rawHtml(`<span class="badge badge-${STATUS_BADGE[n.status] || 'neutral'}">${tStatus(n.status)}</span>`) },
      { label: t('notifications.errorCol'), render: (n) => n.errorMessage || rawHtml('<span class="text-muted">—</span>') },
    ],
    fetchPage: (params) => api.get('/notifications', params),
  });

  document.getElementById('send-notification-btn').addEventListener('click', () => openSendModal(table.refresh));
}
