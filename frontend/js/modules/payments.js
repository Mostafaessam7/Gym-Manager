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

let stripePromise = null;
function loadStripeJs() {
  if (stripePromise) return stripePromise;
  stripePromise = new Promise((resolve, reject) => {
    if (window.Stripe) { resolve(window.Stripe); return; }
    const script = document.createElement('script');
    script.src = 'https://js.stripe.com/v3/';
    script.onload = () => resolve(window.Stripe);
    script.onerror = () => reject(new Error('Failed to load Stripe.js.'));
    document.head.appendChild(script);
  });
  return stripePromise;
}

async function openGatewayPaymentModal(onSaved) {
  const [members, branches] = await Promise.all([api.get('/members', { pageSize: 100 }), api.get('/branches')]);

  const fields = [
    { name: 'memberId', label: 'Member', type: 'select', required: true, options: members.items.map((m) => ({ value: m.id, label: `${m.firstName} ${m.lastName}` })) },
    { name: 'branchId', label: 'Branch', type: 'select', required: true, options: branches.map((b) => ({ value: b.id, label: b.name })) },
    { name: 'amount', label: 'Amount', type: 'number', step: '0.01', required: true },
    { name: 'currency', label: 'Currency', value: 'usd', required: true },
    { name: 'referenceType', label: 'For', type: 'select', options: REFERENCE_TYPES },
    { name: 'receiptEmail', label: 'Receipt Email (optional)', type: 'email' },
  ];
  const formBody = renderForm(fields);
  const cardStep = document.createElement('div');
  cardStep.className = 'hidden';
  cardStep.innerHTML = `
    <p class="text-muted">Enter a test card (e.g. <strong>4242 4242 4242 4242</strong>, any future expiry/CVC) to confirm this payment against Stripe's sandbox.</p>
    <div id="stripe-card-element" style="padding:12px;border:1px solid var(--color-border);border-radius:var(--radius-sm);background:var(--color-surface);"></div>
    <p id="stripe-error" class="text-muted" style="color:var(--color-danger); min-height:1.2em; margin-top:8px;"></p>
  `;

  const body = document.createElement('div');
  body.appendChild(formBody);
  body.appendChild(cardStep);

  let stripe, cardElement;

  openModal({
    title: 'Charge via Stripe',
    wide: true,
    bodyHtml: '',
    onMount: (ctrl) => ctrl.bodyElement.appendChild(body),
    footerButtons: [
      { label: 'Cancel', className: 'btn-secondary', onClick: (ctrl) => ctrl.close() },
      {
        label: 'Continue to Card',
        className: 'btn-primary',
        onClick: async (ctrl) => {
          const values = readForm(formBody, fields);
          values.referenceType = Number(values.referenceType);
          try {
            const intent = await api.post('/payments/gateway-intent', { ...values, referenceId: null });

            const StripeCtor = await loadStripeJs();
            stripe = StripeCtor(intent.publishableKey);
            const elements = stripe.elements();
            cardElement = elements.create('card');

            formBody.classList.add('hidden');
            cardStep.classList.remove('hidden');
            cardElement.mount('#stripe-card-element');

            const footer = ctrl.backdropElement.querySelector('.modal__footer');
            footer.innerHTML = '';
            const cancelBtn = document.createElement('button');
            cancelBtn.className = 'btn btn-secondary';
            cancelBtn.textContent = 'Cancel';
            cancelBtn.addEventListener('click', () => ctrl.close());
            const confirmBtn = document.createElement('button');
            confirmBtn.className = 'btn btn-primary';
            confirmBtn.textContent = 'Confirm Payment';
            confirmBtn.addEventListener('click', async () => {
              confirmBtn.disabled = true;
              confirmBtn.textContent = 'Confirming…';
              const result = await stripe.confirmCardPayment(intent.clientSecret, { payment_method: { card: cardElement } });
              if (result.error) {
                cardStep.querySelector('#stripe-error').textContent = result.error.message;
                confirmBtn.disabled = false;
                confirmBtn.textContent = 'Confirm Payment';
                return;
              }
              toastSuccess('Payment confirmed with Stripe — it will complete once the webhook is received.');
              ctrl.close();
              onSaved();
            });
            footer.appendChild(cancelBtn);
            footer.appendChild(confirmBtn);
          } catch (error) {
            toastError(error.message || 'Failed to start Stripe payment.');
          }
        },
      },
    ],
  });
}

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
        ${authStore.hasPermission('payments:process') ? `
          <div style="display:flex; gap: var(--spacing-2);">
            <button class="btn btn-secondary" id="gateway-payment-btn">Charge via Stripe</button>
            <button class="btn btn-primary" id="record-payment-btn">+ Record Payment</button>
          </div>` : ''}
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
  document.getElementById('gateway-payment-btn')?.addEventListener('click', () => openGatewayPaymentModal(table.refresh));
}
