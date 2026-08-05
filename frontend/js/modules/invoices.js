import { api } from '../api/apiClient.js';
import { createDataTable } from '../components/dataTable.js';
import { toastSuccess, toastError } from '../components/toast.js';
import { authStore } from '../auth/authStore.js';
import { rawHtml } from '../utils/html.js';
import { t, tStatus } from '../i18n/index.js';

export function render(container) {
  container.innerHTML = `
    <div class="card">
      <div class="card-header"><h2>${t('invoices.title')}</h2></div>
      <div id="invoices-table"></div>
    </div>
  `;

  createDataTable(document.getElementById('invoices-table'), {
    searchable: false,
    columns: [
      { label: t('invoices.invoiceNumber'), key: 'invoiceNumber' },
      { label: t('invoices.total'), render: (i) => `${i.totalAmount} ${i.currency}` },
      { label: t('invoices.statusCol'), render: (i) => rawHtml(`<span class="badge badge-${i.status === 'Paid' ? 'success' : i.status === 'Void' ? 'danger' : 'warning'}">${tStatus(i.status)}</span>`) },
      { label: t('invoices.issued'), render: (i) => new Date(i.issuedOnUtc).toLocaleDateString() },
      { label: t('invoices.due'), render: (i) => new Date(i.dueOnUtc).toLocaleDateString() },
    ],
    fetchPage: (params) => api.get('/invoices', params),
    rowActions: authStore.hasPermission('invoices:manage') ? (invoice) => {
      const actions = [];
      if (invoice.status === 'Draft') {
        actions.push({
          label: t('invoices.issue'), onClick: async (row, reload) => {
            try { await api.post(`/invoices/${row.id}/issue`); toastSuccess(t('invoices.invoiceIssued')); reload(); }
            catch (error) { toastError(error.message || t('invoices.issueFailed')); }
          },
        });
      }
      if (invoice.status !== 'Paid' && invoice.status !== 'Void') {
        actions.push({
          label: t('invoices.void'), className: 'btn-danger', onClick: async (row, reload) => {
            try { await api.post(`/invoices/${row.id}/void`); toastSuccess(t('invoices.invoiceVoided')); reload(); }
            catch (error) { toastError(error.message || t('invoices.voidFailed')); }
          },
        });
      }
      return actions;
    } : null,
  });
}
