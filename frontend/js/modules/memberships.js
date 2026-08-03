import { api } from '../api/apiClient.js';
import { createDataTable } from '../components/dataTable.js';
import { openModal } from '../components/modal.js';
import { renderForm, readForm } from '../components/form.js';
import { toastSuccess, toastError } from '../components/toast.js';
import { authStore } from '../auth/authStore.js';
import { rawHtml } from '../utils/html.js';

function planFields(plan = {}) {
  return [
    { name: 'name', label: 'Plan Name', value: plan.name, required: true },
    { name: 'description', label: 'Description', type: 'textarea', value: plan.description, span2: true },
    { name: 'price', label: 'Price', type: 'number', step: '0.01', value: plan.price, required: true },
    { name: 'currency', label: 'Currency', value: plan.currency || 'USD', required: true },
    { name: 'durationInDays', label: 'Duration (days)', type: 'number', value: plan.durationInDays, required: true },
    { name: 'maxFreezeDays', label: 'Max Freeze Days', type: 'number', value: plan.maxFreezeDays ?? 0 },
  ];
}

function openPlanModal(existing, onSaved) {
  const fields = planFields(existing);
  const body = renderForm(fields);

  openModal({
    title: existing ? 'Edit Plan' : 'New Membership Plan',
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
            if (existing) await api.put(`/membership-plans/${existing.id}`, values);
            else await api.post('/membership-plans', values);
            toastSuccess(`Plan ${existing ? 'updated' : 'created'}.`);
            ctrl.close();
            onSaved();
          } catch (error) {
            toastError(error.message || 'Failed to save plan.');
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
    { name: 'memberId', label: 'Member', type: 'select', required: true, options: members.items.map((m) => ({ value: m.id, label: `${m.firstName} ${m.lastName} (${m.memberCode})` })) },
    { name: 'membershipPlanId', label: 'Plan', type: 'select', required: true, options: plans.map((p) => ({ value: p.id, label: `${p.name} — ${p.price} ${p.currency}` })) },
    { name: 'startDate', label: 'Start Date', type: 'date', value: new Date().toISOString().slice(0, 10), required: true },
  ];
  const body = renderForm(fields);

  openModal({
    title: 'Purchase Membership',
    bodyHtml: '',
    onMount: (ctrl) => ctrl.bodyElement.appendChild(body),
    footerButtons: [
      { label: 'Cancel', className: 'btn-secondary', onClick: (ctrl) => ctrl.close() },
      {
        label: 'Purchase',
        className: 'btn-primary',
        onClick: async (ctrl) => {
          try {
            await api.post('/memberships', readForm(body, fields));
            toastSuccess('Membership purchased.');
            ctrl.close();
            onSaved();
          } catch (error) {
            toastError(error.message || 'Failed to purchase membership.');
          }
        },
      },
    ],
  });
}

async function openRenewModal(membership, onSaved) {
  const fields = [
    { name: 'additionalDays', label: 'Additional Days', type: 'number', value: 30, required: true },
    { name: 'amountPaid', label: 'Amount Paid', type: 'number', step: '0.01', required: true },
    { name: 'currency', label: 'Currency', value: 'USD', required: true },
  ];
  const body = renderForm(fields);

  openModal({
    title: 'Renew Membership',
    bodyHtml: '',
    onMount: (ctrl) => ctrl.bodyElement.appendChild(body),
    footerButtons: [
      { label: 'Cancel', className: 'btn-secondary', onClick: (ctrl) => ctrl.close() },
      {
        label: 'Renew',
        className: 'btn-primary',
        onClick: async (ctrl) => {
          try {
            await api.post(`/memberships/${membership.id}/renew`, readForm(body, fields));
            toastSuccess('Membership renewed.');
            ctrl.close();
            onSaved();
          } catch (error) {
            toastError(error.message || 'Failed to renew membership.');
          }
        },
      },
    ],
  });
}

function renderPlansTab(container) {
  container.innerHTML = `
    <div class="card-header">
      ${authStore.hasPermission('memberships:manage') ? '<button class="btn btn-primary" id="new-plan-btn">+ New Plan</button>' : '<span></span>'}
    </div>
    <div id="plans-table"></div>
  `;

  createDataTable(document.getElementById('plans-table'), {
    searchable: false,
    columns: [
      { label: 'Name', key: 'name' },
      { label: 'Price', render: (p) => `${p.price} ${p.currency}` },
      { label: 'Duration', render: (p) => `${p.durationInDays} days` },
      { label: 'Status', render: (p) => rawHtml(`<span class="badge badge-${p.isActive ? 'success' : 'neutral'}">${p.isActive ? 'Active' : 'Inactive'}</span>`) },
    ],
    fetchPage: () => api.get('/membership-plans', { includeInactive: true }),
    rowActions: authStore.hasPermission('memberships:manage') ? (plan) => [
      { label: 'Edit', onClick: (row, reload) => openPlanModal(row, reload) },
      ...(plan.isActive ? [{
        label: 'Deactivate', className: 'btn-danger',
        onClick: async (row, reload) => { await api.post(`/membership-plans/${row.id}/deactivate`); toastSuccess('Plan deactivated.'); reload(); },
      }] : []),
    ] : null,
  });

  document.getElementById('new-plan-btn')?.addEventListener('click', () => openPlanModal(null, () => renderPlansTab(container)));
}

function renderSubscriptionsTab(container) {
  container.innerHTML = `
    <div class="card-header">
      ${authStore.hasPermission('memberships:manage') ? '<button class="btn btn-primary" id="purchase-btn">+ Purchase Membership</button>' : '<span></span>'}
    </div>
    <div id="expiring-table"></div>
  `;

  createDataTable(document.getElementById('expiring-table'), {
    searchable: false,
    columns: [
      { label: 'Member', render: (m) => m.memberId },
      { label: 'Plan', key: 'planNameSnapshot' },
      { label: 'End Date', render: (m) => m.endDate },
      { label: 'Status', render: (m) => rawHtml(`<span class="badge badge-info">${m.status}</span>`) },
    ],
    fetchPage: () => api.get('/memberships/expiring', { withinDays: 14 }),
    rowActions: authStore.hasPermission('memberships:renew') ? (membership) => [
      { label: 'Renew', onClick: (row, reload) => openRenewModal(row, reload) },
    ] : null,
  });

  document.getElementById('purchase-btn')?.addEventListener('click', () => openPurchaseModal(() => renderSubscriptionsTab(container)));
}

export function render(container) {
  container.innerHTML = `
    <div class="card">
      <h2>Memberships</h2>
      <div class="tabs">
        <div class="tab active" data-tab="plans">Plans</div>
        <div class="tab" data-tab="subscriptions">Expiring Subscriptions</div>
      </div>
      <div id="tab-content"></div>
    </div>
  `;

  const tabContent = document.getElementById('tab-content');
  renderPlansTab(tabContent);

  container.querySelectorAll('.tab').forEach((tabEl) => {
    tabEl.addEventListener('click', () => {
      container.querySelectorAll('.tab').forEach((t) => t.classList.remove('active'));
      tabEl.classList.add('active');
      if (tabEl.dataset.tab === 'plans') renderPlansTab(tabContent);
      else renderSubscriptionsTab(tabContent);
    });
  });
}
