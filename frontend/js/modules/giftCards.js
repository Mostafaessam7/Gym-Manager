import { api } from '../api/apiClient.js';
import { createDataTable } from '../components/dataTable.js';
import { openModal, confirmDialog } from '../components/modal.js';
import { renderForm, readForm } from '../components/form.js';
import { toastSuccess, toastError } from '../components/toast.js';
import { authStore } from '../auth/authStore.js';
import { escapeHtml, rawHtml } from '../utils/html.js';
import { t, tStatus } from '../i18n/index.js';

const canManage = () => authStore.hasPermission('gift-cards:manage');
const TXN_BADGE = { Issued: 'info', Redeemed: 'warning', Reloaded: 'success' };

async function openIssueModal(onSaved) {
  const fields = [
    { name: 'initialBalance', label: t('giftCards.initialBalance'), type: 'number', step: '0.01', required: true },
    { name: 'code', label: t('giftCards.code'), value: '' },
    { name: 'issuedToMemberId', label: t('giftCards.issuedToMember'), value: '' },
    { name: 'expiresOnUtc', label: t('giftCards.expiresOn'), type: 'date' },
  ];
  const body = renderForm(fields);

  openModal({
    title: t('giftCards.issueGiftCardTitle'),
    wide: true,
    bodyHtml: '',
    onMount: (ctrl) => ctrl.bodyElement.appendChild(body),
    footerButtons: [
      { label: t('common.cancel'), className: 'btn-secondary', onClick: (ctrl) => ctrl.close() },
      {
        label: t('giftCards.issue'),
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
            toastSuccess(t('giftCards.giftCardIssued'));
            ctrl.close();
            onSaved();
          } catch (error) {
            toastError(error.message || t('giftCards.issueFailed'));
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
      <div><dt>${t('giftCards.detailCode')}</dt><dd>${escapeHtml(card.code)}</dd></div>
      <div><dt>${t('giftCards.detailBalance')}</dt><dd>${card.currentBalance} ${card.currency}</dd></div>
      <div><dt>${t('giftCards.detailInitial')}</dt><dd>${card.initialBalance} ${card.currency}</dd></div>
      <div><dt>${t('giftCards.detailStatus')}</dt><dd><span class="badge badge-${card.isActive ? 'success' : 'neutral'}">${card.isActive ? t('giftCards.active') : t('giftCards.deactivated')}</span></dd></div>
      <div><dt>${t('giftCards.detailExpires')}</dt><dd>${card.expiresOnUtc ? new Date(card.expiresOnUtc).toLocaleDateString() : '—'}</dd></div>
    </dl>
    <h4>${t('giftCards.transactionHistory')}</h4>
    <div class="sub-list">
      ${card.transactions.length ? card.transactions.slice().sort((a, b) => new Date(b.occurredOnUtc) - new Date(a.occurredOnUtc)).map((txn) => `
        <div class="sub-list__row">
          <div><span class="badge badge-${TXN_BADGE[txn.type] || 'neutral'}">${tStatus(txn.type)}</span>
          <span class="sub-list__row-meta">${new Date(txn.occurredOnUtc).toLocaleString()}</span></div>
          <div class="sub-list__row-main">${txn.amount} ${card.currency}</div>
        </div>`).join('') : `<div class="sub-list__row text-muted">${t('giftCards.noTransactions')}</div>`}
    </div>
  `;

  const footerButtons = [{ label: t('common.close'), className: 'btn-secondary', onClick: (ctrl) => ctrl.close() }];
  if (canManage() && card.isActive) {
    footerButtons.unshift(
      { label: t('giftCards.reload'), className: 'btn-secondary', onClick: async (ctrl) => {
        const amountStr = window.prompt(t('giftCards.amountToAddPrompt'));
        const amount = Number(amountStr);
        if (!amountStr || Number.isNaN(amount) || amount <= 0) return;
        try { await api.post(`/gift-cards/${card.id}/reload`, { amount }); toastSuccess(t('giftCards.giftCardReloaded')); ctrl.close(); onChanged(); } catch (e) { toastError(e.message); }
      } },
      { label: t('common.deactivate'), className: 'btn-danger', onClick: async (ctrl) => {
        if (await confirmDialog(t('giftCards.deactivateConfirm', { code: card.code }))) {
          try { await api.post(`/gift-cards/${card.id}/deactivate`); toastSuccess(t('giftCards.giftCardDeactivated')); ctrl.close(); onChanged(); } catch (e) { toastError(e.message); }
        }
      } },
    );
  }

  openModal({ title: t('giftCards.detailTitle', { code: card.code }), wide: true, bodyHtml: '', onMount: (ctrl) => ctrl.bodyElement.appendChild(body), footerButtons });
}

export function render(container) {
  container.innerHTML = `
    <div class="card">
      <div class="card-header">
        <div class="section-title">
          <h2>${t('giftCards.title')}</h2>
          <p class="text-muted">${t('giftCards.subtitle')}</p>
        </div>
        ${canManage() ? `<button class="btn btn-primary" id="issue-btn">${t('giftCards.issueGiftCard')}</button>` : ''}
      </div>
      <div id="gift-cards-table"></div>
    </div>
  `;

  const table = createDataTable(document.getElementById('gift-cards-table'), {
    searchable: false,
    columns: [
      { label: t('giftCards.codeCol'), key: 'code' },
      { label: t('giftCards.balanceCol'), render: (c) => `${c.currentBalance} ${c.currency}` },
      { label: t('giftCards.initialCol'), render: (c) => `${c.initialBalance} ${c.currency}` },
      { label: t('giftCards.statusCol'), render: (c) => rawHtml(`<span class="badge badge-${c.isActive ? 'success' : 'neutral'}">${c.isActive ? t('giftCards.active') : t('giftCards.deactivated')}</span>`) },
      { label: t('giftCards.expiresCol'), render: (c) => (c.expiresOnUtc ? new Date(c.expiresOnUtc).toLocaleDateString() : rawHtml(`<span class="text-muted">${t('giftCards.neverExpires')}</span>`)) },
    ],
    fetchPage: (params) => api.get('/gift-cards', params),
    rowActions: () => [{ label: t('giftCards.view'), onClick: (row, reload) => openDetailModal(row, reload) }],
  });

  document.getElementById('issue-btn')?.addEventListener('click', () => openIssueModal(table.refresh));
}
