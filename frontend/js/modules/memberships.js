import { api } from '../api/apiClient.js';
import { createDataTable } from '../components/dataTable.js';
import { openModal } from '../components/modal.js';
import { renderForm, readForm } from '../components/form.js';
import { toastSuccess, toastError } from '../components/toast.js';
import { authStore } from '../auth/authStore.js';
import { rawHtml } from '../utils/html.js';
import { t, tStatus } from '../i18n/index.js';

function planFields(plan = {}) {
  return [
    { name: 'name', label: t('memberships.planName'), value: plan.name, required: true },
    { name: 'description', label: t('memberships.description'), type: 'textarea', value: plan.description, span2: true, required: true },
    { name: 'price', label: t('memberships.price'), type: 'number', step: '0.01', value: plan.price, required: true },
    { name: 'currency', label: t('memberships.currency'), value: plan.currency || 'USD', required: true },
    { name: 'durationInDays', label: t('memberships.durationDays'), type: 'number', value: plan.durationInDays, required: true },
    { name: 'maxFreezeDays', label: t('memberships.maxFreezeDays'), type: 'number', value: plan.maxFreezeDays ?? 0 },
  ];
}

function openPlanModal(existing, onSaved) {
  const fields = planFields(existing || {});
  const body = renderForm(fields);

  openModal({
    title: existing ? t('memberships.editPlan') : t('memberships.newPlanTitle'),
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
            if (existing) await api.put(`/membership-plans/${existing.id}`, values);
            else await api.post('/membership-plans', values);
            toastSuccess(existing ? t('memberships.planUpdated') : t('memberships.planCreated'));
            ctrl.close();
            onSaved();
          } catch (error) {
            toastError(error.message || t('memberships.saveFailed'));
          }
        },
      },
    ],
  });
}

async function openPurchaseModal(onSaved) {
  const [members, plans] = await Promise.all([
    api.get('/members', { pageSize: 100 }),
    api.get('/membership-plans'),
  ]);

  const fields = [
    { name: 'memberId', label: t('memberships.memberCol'), type: 'select', required: true, options: members.items.map((m) => ({ value: m.id, label: `${m.firstName} ${m.lastName} (${m.memberCode})` })) },
    { name: 'membershipPlanId', label: t('memberships.plan'), type: 'select', required: true, options: plans.map((p) => ({ value: p.id, label: `${p.name} — ${p.price} ${p.currency}` })) },
    { name: 'startDate', label: t('memberships.startDate'), type: 'date', value: new Date().toISOString().slice(0, 10), required: true },
  ];
  const body = renderForm(fields);

  openModal({
    title: t('memberships.purchaseMembershipTitle'),
    bodyHtml: '',
    onMount: (ctrl) => ctrl.bodyElement.appendChild(body),
    footerButtons: [
      { label: t('common.cancel'), className: 'btn-secondary', onClick: (ctrl) => ctrl.close() },
      {
        label: t('memberships.purchase'),
        className: 'btn-primary',
        onClick: async (ctrl) => {
          try {
            await api.post('/memberships', readForm(body, fields));
            toastSuccess(t('memberships.membershipPurchased'));
            ctrl.close();
            onSaved();
          } catch (error) {
            toastError(error.message || t('memberships.purchaseFailed'));
          }
        },
      },
    ],
  });
}

async function openRenewModal(membership, onSaved) {
  const fields = [
    { name: 'additionalDays', label: t('memberships.additionalDays'), type: 'number', value: 30, required: true },
    { name: 'amountPaid', label: t('memberships.amountPaid'), type: 'number', step: '0.01', required: true },
    { name: 'currency', label: t('memberships.currency'), value: 'USD', required: true },
  ];
  const body = renderForm(fields);

  openModal({
    title: t('memberships.renewMembershipTitle'),
    bodyHtml: '',
    onMount: (ctrl) => ctrl.bodyElement.appendChild(body),
    footerButtons: [
      { label: t('common.cancel'), className: 'btn-secondary', onClick: (ctrl) => ctrl.close() },
      {
        label: t('memberships.renew'),
        className: 'btn-primary',
        onClick: async (ctrl) => {
          try {
            await api.post(`/memberships/${membership.id}/renew`, readForm(body, fields));
            toastSuccess(t('memberships.membershipRenewed'));
            ctrl.close();
            onSaved();
          } catch (error) {
            toastError(error.message || t('memberships.renewFailed'));
          }
        },
      },
    ],
  });
}

function renderPlansTab(container) {
  container.innerHTML = `
    <div class="card-header">
      ${authStore.hasPermission('memberships:manage') ? `<button class="btn btn-primary" id="new-plan-btn">${t('memberships.newPlan')}</button>` : '<span></span>'}
    </div>
    <div id="plans-table"></div>
  `;

  createDataTable(document.getElementById('plans-table'), {
    searchable: false,
    columns: [
      { label: t('memberships.name'), key: 'name' },
      { label: t('memberships.priceCol'), render: (p) => `${p.price} ${p.currency}` },
      { label: t('memberships.durationCol'), render: (p) => t('memberships.durationDaysValue', { days: p.durationInDays }) },
      { label: t('memberships.statusCol'), render: (p) => rawHtml(`<span class="badge badge-${p.isActive ? 'success' : 'neutral'}">${p.isActive ? tStatus('Active') : tStatus('Inactive')}</span>`) },
    ],
    fetchPage: () => api.get('/membership-plans', { includeInactive: true }),
    rowActions: authStore.hasPermission('memberships:manage') ? (plan) => [
      { label: t('common.edit'), onClick: (row, reload) => openPlanModal(row, reload) },
      ...(plan.isActive ? [{
        label: t('common.deactivate'), className: 'btn-danger',
        onClick: async (row, reload) => { await api.post(`/membership-plans/${row.id}/deactivate`); toastSuccess(t('memberships.planDeactivated')); reload(); },
      }] : []),
    ] : null,
  });

  document.getElementById('new-plan-btn')?.addEventListener('click', () => openPlanModal(null, () => renderPlansTab(container)));
}

function renderSubscriptionsTab(container) {
  container.innerHTML = `
    <div class="card-header">
      ${authStore.hasPermission('memberships:manage') ? `<button class="btn btn-primary" id="purchase-btn">${t('memberships.purchaseMembership')}</button>` : '<span></span>'}
    </div>
    <div id="expiring-table"></div>
  `;

  createDataTable(document.getElementById('expiring-table'), {
    searchable: false,
    columns: [
      { label: t('memberships.memberCol'), render: (m) => m.memberId },
      { label: t('memberships.plan'), key: 'planNameSnapshot' },
      { label: t('memberships.endDate'), render: (m) => m.endDate },
      { label: t('memberships.statusCol'), render: (m) => rawHtml(`<span class="badge badge-info">${tStatus(m.status)}</span>`) },
    ],
    fetchPage: () => api.get('/memberships/expiring', { withinDays: 14 }),
    rowActions: authStore.hasPermission('memberships:renew') ? (membership) => [
      { label: t('memberships.renew'), onClick: (row, reload) => openRenewModal(row, reload) },
    ] : null,
  });

  document.getElementById('purchase-btn')?.addEventListener('click', () => openPurchaseModal(() => renderSubscriptionsTab(container)));
}

export function render(container) {
  container.innerHTML = `
    <div class="card">
      <h2>${t('memberships.title')}</h2>
      <div class="tabs">
        <div class="tab active" data-tab="plans">${t('memberships.plans')}</div>
        <div class="tab" data-tab="subscriptions">${t('memberships.expiringSubscriptions')}</div>
      </div>
      <div id="tab-content"></div>
    </div>
  `;

  const tabContent = document.getElementById('tab-content');
  renderPlansTab(tabContent);

  container.querySelectorAll('.tab').forEach((tabEl) => {
    tabEl.addEventListener('click', () => {
      container.querySelectorAll('.tab').forEach((t2) => t2.classList.remove('active'));
      tabEl.classList.add('active');
      if (tabEl.dataset.tab === 'plans') renderPlansTab(tabContent);
      else renderSubscriptionsTab(tabContent);
    });
  });
}
