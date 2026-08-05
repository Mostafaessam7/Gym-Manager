import { api } from '../api/apiClient.js';
import { createDataTable } from '../components/dataTable.js';
import { openModal, confirmDialog } from '../components/modal.js';
import { renderForm, readForm } from '../components/form.js';
import { toastSuccess, toastError } from '../components/toast.js';
import { authStore } from '../auth/authStore.js';
import { escapeHtml, rawHtml } from '../utils/html.js';

const canManage = () => authStore.hasPermission('gift-cards:manage');
const TXN_BADGE = { Issued: 'info', Redeemed: 'warning', Reloaded: 'success' };

async function openIssueModal(onSaved) {
  const fields = [
    { name: 'initialBalance', label: 'Initial Balance', type: 'number', step: '0.01', required: true },
    { name: 'code', label: 'Code (optional, auto-generated if blank)', value: '' },
    { name: 'issuedToMemberId', label: 'Issued to Member Id (optional)', value: '' },
    { name: 'expiresOnUtc', label: 'Expires On', type: 'date' },
  ];
  const body = renderForm(fields);

  openModal({
    title: 'Issue Gift Card',
    wide: true,
    bodyHtml: '',
    onMount: (ctrl) => ctrl.bodyElement.appendChild(body),
    footerButtons: [
      { label: 'Cancel', className: 'btn-secondary', onClick: (ctrl) => ctrl.close() },
      {
        label: 'Issue',
        className: 'btn-primary',
        onClick: async (ctrl) => {
          const values = readForm(body, fields);
          try {
            await api.post('/gift-cards', {
              initialBalance: values.initialBalance,
              code: values.code || null,
              issuedToMemberId: values.issuedToMemberId || null,
              expiresOnUtc: values.expiresOnUtc ? new Date(values.expiresOnUtc).toISOString() : null,
            });
            toastSuccess('Gift card issued.');
            ctrl.close();
            onSaved();
          } catch (error) {
            toastError(error.message || 'Failed to issue gift card.');
          }
        },
      },
    ],
  });
}

async function openDetailModal(cardSummary, onChanged) {
  const card = await api.get(`/gift-cards/${cardSummary.code}`);

  const body = document.createElement('div');
  body.innerHTML = `
    <dl class="detail-list">
      <div><dt>Code</dt><dd>${escapeHtml(card.code)}</dd></div>
      <div><dt>Balance</dt><dd>${card.currentBalance} ${card.currency}</dd></div>
      <div><dt>Initial</dt><dd>${card.initialBalance} ${card.currency}</dd></div>
      <div><dt>Status</dt><dd><span class="badge badge-${card.isActive ? 'success' : 'neutral'}">${card.isActive ? 'Active' : 'Deactivated'}</span></dd></div>
      <div><dt>Expires</dt><dd>${card.expiresOnUtc ? new Date(card.expiresOnUtc).toLocaleDateString() : '—'}</dd></div>
    </dl>
    <h4>Transaction History</h4>
    <div class="sub-list">
      ${card.transactions.length ? card.transactions.slice().sort((a, b) => new Date(b.occurredOnUtc) - new Date(a.occurredOnUtc)).map((t) => `
        <div class="sub-list__row">
          <div><span class="badge badge-${TXN_BADGE[t.type] || 'neutral'}">${t.type}</span>
          <span class="sub-list__row-meta">${new Date(t.occurredOnUtc).toLocaleString()}</span></div>
          <div class="sub-list__row-main">${t.amount} ${card.currency}</div>
        </div>`).join('') : '<div class="sub-list__row text-muted">No transactions yet.</div>'}
    </div>
  `;

  const footerButtons = [{ label: 'Close', className: 'btn-secondary', onClick: (ctrl) => ctrl.close() }];
  if (canManage() && card.isActive) {
    footerButtons.unshift(
      { label: 'Reload', className: 'btn-secondary', onClick: async (ctrl) => {
        const amountStr = window.prompt('Amount to add:');
        const amount = Number(amountStr);
        if (!amountStr || Number.isNaN(amount) || amount <= 0) return;
        try { await api.post(`/gift-cards/${card.id}/reload`, { amount }); toastSuccess('Gift card reloaded.'); ctrl.close(); onChanged(); } catch (e) { toastError(e.message); }
      } },
      { label: 'Deactivate', className: 'btn-danger', onClick: async (ctrl) => {
        if (await confirmDialog(`Deactivate gift card ${card.code}?`)) {
          try { await api.post(`/gift-cards/${card.id}/deactivate`); toastSuccess('Gift card deactivated.'); ctrl.close(); onChanged(); } catch (e) { toastError(e.message); }
        }
      } },
    );
  }

  openModal({ title: `Gift Card ${card.code}`, wide: true, bodyHtml: '', onMount: (ctrl) => ctrl.bodyElement.appendChild(body), footerButtons });
}

export function render(container) {
  container.innerHTML = `
    <div class="card">
      <div class="card-header">
        <div class="section-title">
          <h2>Gift Cards</h2>
          <p class="text-muted">Issue, reload, and track balances redeemable at checkout.</p>
        </div>
        ${canManage() ? '<button class="btn btn-primary" id="issue-btn">+ Issue Gift Card</button>' : ''}
      </div>
      <div id="gift-cards-table"></div>
    </div>
  `;

  const table = createDataTable(document.getElementById('gift-cards-table'), {
    searchable: false,
    columns: [
      { label: 'Code', key: 'code' },
      { label: 'Balance', render: (c) => `${c.currentBalance} ${c.currency}` },
      { label: 'Initial', render: (c) => `${c.initialBalance} ${c.currency}` },
      { label: 'Status', render: (c) => rawHtml(`<span class="badge badge-${c.isActive ? 'success' : 'neutral'}">${c.isActive ? 'Active' : 'Deactivated'}</span>`) },
      { label: 'Expires', render: (c) => (c.expiresOnUtc ? new Date(c.expiresOnUtc).toLocaleDateString() : rawHtml('<span class="text-muted">Never</span>')) },
    ],
    fetchPage: (params) => api.get('/gift-cards', params),
    rowActions: () => [{ label: 'View', onClick: (row, reload) => openDetailModal(row, reload) }],
  });

  document.getElementById('issue-btn')?.addEventListener('click', () => openIssueModal(table.refresh));
}
