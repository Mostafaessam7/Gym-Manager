import { api } from '../api/apiClient.js';
import { createDataTable } from '../components/dataTable.js';
import { openModal, confirmDialog } from '../components/modal.js';
import { renderForm, readForm } from '../components/form.js';
import { toastSuccess, toastError } from '../components/toast.js';
import { authStore } from '../auth/authStore.js';
import { rawHtml, safeUrl } from '../utils/html.js';
import { t, tStatus } from '../i18n/index.js';

const canManage = () => authStore.hasPermission('expenses:manage');

// Mirrors GymManager.Domain.Expenses.ExpenseCategory's declaration order exactly (the API serializes the
// enum as its name, e.g. "Rent", not its numeric value — this list maps name <-> select value both ways).
const CATEGORY_NAMES = ['Rent', 'Utilities', 'Salaries', 'Equipment', 'Maintenance', 'Marketing', 'Other'];
const CATEGORIES = () => [
  { value: 0, label: t('expenses.categoryRent') },
  { value: 1, label: t('expenses.categoryUtilities') },
  { value: 2, label: t('expenses.categorySalaries') },
  { value: 3, label: t('expenses.categoryEquipment') },
  { value: 4, label: t('expenses.categoryMaintenance') },
  { value: 5, label: t('expenses.categoryMarketing') },
  { value: 6, label: t('expenses.categoryOther') },
];

let branchOptionsCache = null;
async function branchOptions() {
  if (branchOptionsCache) return branchOptionsCache;
  try {
    const branches = await api.get('/branches');
    branchOptionsCache = branches.map((b) => ({ value: b.id, label: b.name }));
  } catch {
    branchOptionsCache = [];
  }
  return branchOptionsCache;
}

function branchName(branches, id) {
  return branches.find((b) => b.value === id)?.label || `${id.slice(0, 8)}…`;
}

function expenseFields(branches, expense = {}) {
  const fields = [
    { name: 'category', label: t('expenses.category'), type: 'select', value: expense.categoryValue ?? 0, options: CATEGORIES() },
    { name: 'description', label: t('common.description'), value: expense.description, required: true, span2: true },
    { name: 'amount', label: t('common.amount'), type: 'number', step: '0.01', value: expense.amount, required: true },
    { name: 'currency', label: t('common.currency'), value: expense.currency || 'USD', required: true },
    { name: 'expenseDate', label: t('expenses.expenseDate'), type: 'date', value: expense.expenseDate, required: true },
    { name: 'paidTo', label: t('expenses.paidTo'), value: expense.paidTo, required: true },
    { name: 'receiptUrl', label: t('expenses.receiptUrl'), value: expense.receiptUrl, span2: true },
  ];
  if (!expense.id) {
    fields.unshift({ name: 'branchId', label: t('common.branch'), type: 'select', required: true, options: branches });
  }
  return fields;
}

async function openExpenseModal(existing, onSaved) {
  const branches = await branchOptions();
  const fields = expenseFields(branches, existing || {});
  const body = renderForm(fields);

  openModal({
    title: existing ? t('expenses.editExpenseTitle') : t('expenses.newExpenseTitle'),
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
            if (existing) {
              await api.put(`/expenses/${existing.id}`, { ...values, branchId: existing.branchId, receiptUrl: values.receiptUrl || null });
              toastSuccess(t('expenses.expenseUpdated'));
            } else {
              await api.post('/expenses', { ...values, receiptUrl: values.receiptUrl || null });
              toastSuccess(t('expenses.expenseRecorded'));
            }
            ctrl.close();
            onSaved();
          } catch (error) {
            toastError(error.message || t('expenses.saveFailed'));
          }
        },
      },
    ],
  });
}

export async function render(container) {
  const branches = await branchOptions();

  container.innerHTML = `
    <div class="card">
      <div class="card-header">
        <div class="section-title">
          <h2>${t('expenses.title')}</h2>
          <p class="text-muted">${t('expenses.subtitle')}</p>
        </div>
        ${canManage() ? `<button class="btn btn-primary" id="new-expense-btn">${t('expenses.newExpense')}</button>` : ''}
      </div>
      <div id="expenses-table"></div>
    </div>
  `;

  const table = createDataTable(document.getElementById('expenses-table'), {
    searchable: false,
    columns: [
      { label: t('expenses.dateCol'), render: (e) => e.expenseDate },
      { label: t('expenses.categoryCol'), render: (e) => tStatus(e.category) },
      { label: t('common.description'), key: 'description' },
      { label: t('expenses.paidToCol'), key: 'paidTo' },
      { label: t('expenses.branchCol'), render: (e) => branchName(branches, e.branchId) },
      { label: t('expenses.amountCol'), render: (e) => `${e.amount} ${e.currency}` },
      { label: t('expenses.receiptCol'), render: (e) => (e.receiptUrl ? rawHtml(`<a href="${safeUrl(e.receiptUrl)}" target="_blank" rel="noopener">${t('expenses.viewReceipt')}</a>`) : rawHtml('<span class="text-muted">—</span>')) },
    ],
    fetchPage: (params) => api.get('/expenses', params),
    rowActions: canManage() ? (expense) => [
      {
        label: t('common.edit'),
        onClick: (row, reload) => openExpenseModal({
          ...row,
          categoryValue: CATEGORY_NAMES.indexOf(row.category),
        }, reload),
      },
      {
        label: t('common.delete'), className: 'btn-danger',
        onClick: async (row, reload) => {
          if (await confirmDialog(t('expenses.deleteConfirm', { description: row.description }))) {
            await api.delete(`/expenses/${row.id}`);
            toastSuccess(t('expenses.expenseDeleted'));
            reload();
          }
        },
      },
    ] : null,
  });

  document.getElementById('new-expense-btn')?.addEventListener('click', () => openExpenseModal(null, table.refresh));
}
