import { api } from '../api/apiClient.js';
import { createDataTable } from '../components/dataTable.js';
import { openModal, confirmDialog } from '../components/modal.js';
import { renderForm, readForm } from '../components/form.js';
import { toastSuccess, toastError } from '../components/toast.js';
import { authStore } from '../auth/authStore.js';
import { rawHtml } from '../utils/html.js';
import { t, tStatus } from '../i18n/index.js';

const METHODS = () => [
  { value: 0, label: t('payments.methodCash') }, { value: 1, label: t('payments.methodCard') },
  { value: 2, label: t('payments.methodBankTransfer') }, { value: 3, label: t('payments.methodOther') },
];
const REFERENCE_TYPES = () => [
  { value: 0, label: t('payments.refMembershipPurchase') }, { value: 1, label: t('payments.refMembershipRenewal') },
  { value: 2, label: t('payments.refClassBooking') }, { value: 3, label: t('payments.refProductSale') }, { value: 4, label: t('payments.refOther') },
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

// Matches the backend's PaymentGatewayProvider enum (GymManager.Domain.Payments).
const GATEWAY_PROVIDERS = () => [
  { value: 1, label: t('payments.gatewayStripe') },
  { value: 2, label: t('payments.gatewayPaymob') },
  { value: 3, label: t('payments.gatewayFawry') },
];

async function openGatewayPaymentModal(onSaved) {
  const [members, branches] = await Promise.all([api.get('/members', { pageSize: 100 }), api.get('/branches')]);

  const fields = [
    { name: 'memberId', label: t('payments.member'), type: 'select', required: true, options: members.items.map((m) => ({ value: m.id, label: `${m.firstName} ${m.lastName}` })) },
    { name: 'branchId', label: t('payments.branch'), type: 'select', required: true, options: branches.map((b) => ({ value: b.id, label: b.name })) },
    { name: 'provider', label: t('payments.gatewayProvider'), type: 'select', required: true, options: GATEWAY_PROVIDERS() },
    { name: 'amount', label: t('payments.amount'), type: 'number', step: '0.01', required: true },
    { name: 'currency', label: t('payments.currency'), value: 'usd', required: true },
    { name: 'referenceType', label: t('payments.forField'), type: 'select', options: REFERENCE_TYPES() },
    { name: 'receiptEmail', label: t('payments.receiptEmail'), type: 'email' },
  ];
  const formBody = renderForm(fields);
  const cardStep = document.createElement('div');
  cardStep.className = 'hidden';
  cardStep.innerHTML = `
    <p class="text-muted">${t('payments.testCardHint')}</p>
    <div id="stripe-card-element" style="padding:12px;border:1px solid var(--color-border);border-radius:var(--radius-sm);background:var(--color-surface);"></div>
    <p id="stripe-error" class="text-muted" style="color:var(--color-danger); min-height:1.2em; margin-top:8px;"></p>
  `;

  const body = document.createElement('div');
  body.appendChild(formBody);
  body.appendChild(cardStep);

  let stripe, cardElement;

  openModal({
    title: t('payments.chargeViaStripeTitle'),
    wide: true,
    bodyHtml: '',
    onMount: (ctrl) => ctrl.bodyElement.appendChild(body),
    footerButtons: [
      { label: t('common.cancel'), className: 'btn-secondary', onClick: (ctrl) => ctrl.close() },
      {
        label: t('payments.continueToCard'),
        className: 'btn-primary',
        onClick: async (ctrl) => {
          const values = readForm(formBody, fields);
          const provider = Number(values.provider);
          try {
            const intent = await api.post('/payments/gateway-intent', { ...values, provider, referenceId: null });

            if (provider === 2) {
              // Paymob: clientSecret is the iframe URL — open it in a new tab for the member to enter card
              // details; this backend never sees card data either way.
              window.open(intent.clientSecret, '_blank', 'noopener');
              showFollowUpStep(ctrl, formBody, cardStep, t('payments.paymobRedirectHint'), () => window.open(intent.clientSecret, '_blank', 'noopener'), t('payments.openPaymobWindow'));
              onSaved();
              return;
            }

            if (provider === 3) {
              // Fawry: clientSecret is the reference number itself — nothing to redirect to, just display it.
              showFollowUpStep(ctrl, formBody, cardStep, `${t('payments.fawryReferenceHint')} ${t('payments.fawryReferenceLabel')}: ${intent.clientSecret}`);
              onSaved();
              return;
            }

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
            cancelBtn.textContent = t('common.cancel');
            cancelBtn.addEventListener('click', () => ctrl.close());
            const confirmBtn = document.createElement('button');
            confirmBtn.className = 'btn btn-primary';
            confirmBtn.textContent = t('payments.confirmPaymentBtn');
            confirmBtn.addEventListener('click', async () => {
              confirmBtn.disabled = true;
              confirmBtn.textContent = t('payments.confirming');
              const result = await stripe.confirmCardPayment(intent.clientSecret, { payment_method: { card: cardElement } });
              if (result.error) {
                cardStep.querySelector('#stripe-error').textContent = result.error.message;
                confirmBtn.disabled = false;
                confirmBtn.textContent = t('payments.confirmPaymentBtn');
                return;
              }
              toastSuccess(t('payments.paymentConfirmedStripe'));
              ctrl.close();
              onSaved();
            });
            footer.appendChild(cancelBtn);
            footer.appendChild(confirmBtn);
          } catch (error) {
            toastError(error.message || t('payments.startStripeFailed'));
          }
        },
      },
    ],
  });
}

// Shared by the Paymob/Fawry branches above: replaces the form with a plain informational step (no card
// element needed for either — Paymob's card entry happens in its own iframe/window, Fawry has no card step
// at all), plus a single "Done" button.
function showFollowUpStep(ctrl, formBody, cardStep, message, onReopen, reopenLabel) {
  formBody.classList.add('hidden');
  cardStep.classList.remove('hidden');
  cardStep.innerHTML = `<p class="text-muted">${message}</p>`;

  const footer = ctrl.backdropElement.querySelector('.modal__footer');
  footer.innerHTML = '';
  if (onReopen) {
    const reopenBtn = document.createElement('button');
    reopenBtn.className = 'btn btn-secondary';
    reopenBtn.textContent = reopenLabel;
    reopenBtn.addEventListener('click', onReopen);
    footer.appendChild(reopenBtn);
  }
  const doneBtn = document.createElement('button');
  doneBtn.className = 'btn btn-primary';
  doneBtn.textContent = t('payments.done');
  doneBtn.addEventListener('click', () => ctrl.close());
  footer.appendChild(doneBtn);
}

async function openRecordPaymentModal(onSaved) {
  const [members, branches] = await Promise.all([api.get('/members', { pageSize: 100 }), api.get('/branches')]);

  const fields = [
    { name: 'memberId', label: t('payments.member'), type: 'select', required: true, options: members.items.map((m) => ({ value: m.id, label: `${m.firstName} ${m.lastName}` })) },
    { name: 'branchId', label: t('payments.branch'), type: 'select', required: true, options: branches.map((b) => ({ value: b.id, label: b.name })) },
    { name: 'amount', label: t('payments.amount'), type: 'number', step: '0.01', required: true },
    { name: 'currency', label: t('payments.currency'), value: 'USD', required: true },
    { name: 'method', label: t('payments.method'), type: 'select', options: METHODS() },
    { name: 'referenceType', label: t('payments.forField2'), type: 'select', options: REFERENCE_TYPES() },
  ];
  const body = renderForm(fields);

  openModal({
    title: t('payments.recordPaymentTitle'),
    wide: true,
    bodyHtml: '',
    onMount: (ctrl) => ctrl.bodyElement.appendChild(body),
    footerButtons: [
      { label: t('common.cancel'), className: 'btn-secondary', onClick: (ctrl) => ctrl.close() },
      {
        label: t('payments.record'),
        className: 'btn-primary',
        onClick: async (ctrl) => {
          try {
            await api.post('/payments', readForm(body, fields));
            toastSuccess(t('payments.paymentRecorded'));
            ctrl.close();
            onSaved();
          } catch (error) {
            toastError(error.message || t('payments.recordFailed'));
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
        <h2>${t('payments.title')}</h2>
        ${authStore.hasPermission('payments:process') ? `
          <div style="display:flex; gap: var(--spacing-2);">
            <button class="btn btn-secondary" id="gateway-payment-btn">${t('payments.chargeViaStripe')}</button>
            <button class="btn btn-primary" id="record-payment-btn">${t('payments.recordPaymentBtn')}</button>
          </div>` : ''}
      </div>
      <div id="payments-table"></div>
    </div>
  `;

  const table = createDataTable(document.getElementById('payments-table'), {
    searchable: false,
    columns: [
      { label: t('payments.amountCol'), render: (p) => `${p.amount} ${p.currency}` },
      { label: t('payments.methodCol'), render: (p) => tStatus(p.method) },
      { label: t('payments.statusCol'), render: (p) => rawHtml(`<span class="badge badge-${p.status === 'Completed' ? 'success' : p.status === 'Refunded' ? 'danger' : 'warning'}">${tStatus(p.status)}</span>`) },
      { label: t('payments.dateCol'), render: (p) => new Date(p.createdOnUtc).toLocaleString() },
    ],
    fetchPage: (params) => api.get('/payments', params),
    rowActions: authStore.hasPermission('payments:refund') ? (payment) => (payment.status === 'Completed' ? [{
      label: t('payments.refund'), className: 'btn-danger',
      onClick: async (row, reload) => {
        if (await confirmDialog(t('payments.refundConfirm'))) {
          await api.post(`/payments/${row.id}/refund`);
          toastSuccess(t('payments.paymentRefunded'));
          reload();
        }
      },
    }] : []) : null,
  });

  document.getElementById('record-payment-btn')?.addEventListener('click', () => openRecordPaymentModal(table.refresh));
  document.getElementById('gateway-payment-btn')?.addEventListener('click', () => openGatewayPaymentModal(table.refresh));
}
