import { api } from '../api/apiClient.js';
import { createDataTable } from '../components/dataTable.js';
import { openModal, confirmDialog } from '../components/modal.js';
import { renderForm, readForm } from '../components/form.js';
import { toastSuccess, toastError } from '../components/toast.js';
import { authStore } from '../auth/authStore.js';
import { rawHtml } from '../utils/html.js';

const METHODS = [{ value: 0, label: 'Cash' }, { value: 1, label: 'Card' }, { value: 2, label: 'Bank Transfer' }, { value: 3, label: 'Other' }];
const REFERENCE_TYPES = [
  { value: 0, label: 'Membership Purchase' }, { value: 1, label: 'Membership Renewal' },
  { value: 2, label: 'Class Booking' }, { value: 3, label: 'Product Sale' }, { value: 4, label: 'Other' },
];

async function openRecordPaymentModal(onSaved) {
  const [members, branches] = await Promise.all([api.get('/members', { pageSize: 100 }), api.get('/branches')]);

  const fields = [
    { name: 'memberId', label: 'Member', type: 'select', required: true, options: members.items.map((m) => ({ value: m.id, label: `${m.firstName} ${m.lastName}` })) },
    { name: 'branchId', label: 'Branch', type: 'select', required: true, options: branches.map((b) => ({ value: b.id, label: b.name })) },
    { name: 'amount', label: 'Amount', type: 'number', step: '0.01', required: true },
    { name: 'currency', label: 'Currency', value: 'USD', required: true },
    { name: 'method', label: 'Method', type: 'select', options: METHODS },
    { name: 'referenceType', label: 'For', type: 'select', options: REFERENCE_TYPES },
  ];
  const body = renderForm(fields);

  openModal({
    title: 'Record Payment',
    wide: true,
    bodyHtml: '',
    onMount: (ctrl) => ctrl.bodyElement.appendChild(body),
    footerButtons: [
      { label: 'Cancel', className: 'btn-secondary', onClick: (ctrl) => ctrl.close() },
      {
        label: 'Record',
        className: 'btn-primary',
        onClick: async (ctrl) => {
          try {
            await api.post('/payments', readForm(body, fields));
            toastSuccess('Payment recorded.');
            ctrl.close();
            onSaved();
          } catch (error) {
            toastError(error.message || 'Failed to record payment.');
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
        <h2>Payments</h2>
        ${authStore.hasPermission('payments:process') ? '<button class="btn btn-primary" id="record-payment-btn">+ Record Payment</button>' : ''}
      </div>
      <div id="payments-table"></div>
    </div>
  `;

  const table = createDataTable(document.getElementById('payments-table'), {
    searchable: false,
    columns: [
      { label: 'Amount', render: (p) => `${p.amount} ${p.currency}` },
      { label: 'Method', key: 'method' },
      { label: 'Status', render: (p) => rawHtml(`<span class="badge badge-${p.status === 'Completed' ? 'success' : p.status === 'Refunded' ? 'danger' : 'warning'}">${p.status}</span>`) },
      { label: 'Date', render: (p) => new Date(p.createdOnUtc).toLocaleString() },
    ],
    fetchPage: (params) => api.get('/payments', params),
    rowActions: authStore.hasPermission('payments:refund') ? (payment) => (payment.status === 'Completed' ? [{
      label: 'Refund', className: 'btn-danger',
      onClick: async (row, reload) => {
        if (await confirmDialog('Refund this payment?')) {
          await api.post(`/payments/${row.id}/refund`);
          toastSuccess('Payment refunded.');
          reload();
        }
      },
    }] : []) : null,
  });

  document.getElementById('record-payment-btn')?.addEventListener('click', () => openRecordPaymentModal(table.refresh));
}
