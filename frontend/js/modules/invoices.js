import { api } from '../api/apiClient.js';
import { createDataTable } from '../components/dataTable.js';
import { toastSuccess, toastError } from '../components/toast.js';
import { authStore } from '../auth/authStore.js';
import { rawHtml } from '../utils/html.js';

export function render(container) {
  container.innerHTML = `
    <div class="card">
      <div class="card-header"><h2>Invoices</h2></div>
      <div id="invoices-table"></div>
    </div>
  `;

  createDataTable(document.getElementById('invoices-table'), {
    searchable: false,
    columns: [
      { label: 'Invoice #', key: 'invoiceNumber' },
      { label: 'Total', render: (i) => `${i.totalAmount} ${i.currency}` },
      { label: 'Status', render: (i) => rawHtml(`<span class="badge badge-${i.status === 'Paid' ? 'success' : i.status === 'Void' ? 'danger' : 'warning'}">${i.status}</span>`) },
      { label: 'Issued', render: (i) => new Date(i.issuedOnUtc).toLocaleDateString() },
      { label: 'Due', render: (i) => new Date(i.dueOnUtc).toLocaleDateString() },
    ],
    fetchPage: (params) => api.get('/invoices', params),
    rowActions: authStore.hasPermission('invoices:manage') ? (invoice) => {
      const actions = [];
      if (invoice.status === 'Draft') {
        actions.push({
          label: 'Issue', onClick: async (row, reload) => {
            try { await api.post(`/invoices/${row.id}/issue`); toastSuccess('Invoice issued.'); reload(); }
            catch (error) { toastError(error.message || 'Failed to issue invoice.'); }
          },
        });
      }
      if (invoice.status !== 'Paid' && invoice.status !== 'Void') {
        actions.push({
          label: 'Void', className: 'btn-danger', onClick: async (row, reload) => {
            try { await api.post(`/invoices/${row.id}/void`); toastSuccess('Invoice voided.'); reload(); }
            catch (error) { toastError(error.message || 'Failed to void invoice.'); }
          },
        });
      }
      return actions;
    } : null,
  });
}
