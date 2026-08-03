import { api } from '../api/apiClient.js';
import { createDataTable } from '../components/dataTable.js';
import { openModal } from '../components/modal.js';
import { renderForm, readForm } from '../components/form.js';
import { toastSuccess, toastError } from '../components/toast.js';
import { authStore } from '../auth/authStore.js';
import { rawHtml, escapeHtml } from '../utils/html.js';

const CATEGORIES = [
  { value: 0, label: 'Supplement' }, { value: 1, label: 'Apparel' },
  { value: 2, label: 'Accessory' }, { value: 3, label: 'Beverage' }, { value: 4, label: 'Other' },
];

async function openProductModal(existing, onSaved) {
  const fields = [
    { name: 'name', label: 'Product Name', value: existing?.name, required: true },
    { name: 'description', label: 'Description', type: 'textarea', value: existing?.description, span2: true },
    { name: 'category', label: 'Category', type: 'select', value: existing?.category, options: CATEGORIES },
    { name: 'sku', label: 'SKU', value: existing?.sku, required: true },
    { name: 'price', label: 'Price', type: 'number', step: '0.01', value: existing?.price, required: true },
    { name: 'costPrice', label: 'Cost Price', type: 'number', step: '0.01', value: existing?.costPrice, required: true },
    { name: 'currency', label: 'Currency', value: existing?.currency || 'USD', required: true },
    { name: 'reorderThreshold', label: 'Reorder Threshold', type: 'number', value: existing?.reorderThreshold ?? 5 },
  ];

  if (!existing) {
    const branches = await api.get('/branches');
    fields.push({ name: 'branchId', label: 'Branch', type: 'select', required: true, options: branches.map((b) => ({ value: b.id, label: b.name })) });
    fields.push({ name: 'initialStock', label: 'Initial Stock', type: 'number', value: 0 });
  }

  const body = renderForm(fields);

  openModal({
    title: existing ? 'Edit Product' : 'New Product',
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
            if (existing) await api.put(`/products/${existing.id}`, values);
            else await api.post('/products', values);
            toastSuccess(`Product ${existing ? 'updated' : 'created'}.`);
            ctrl.close();
            onSaved();
          } catch (error) {
            toastError(error.message || 'Failed to save product.');
          }
        },
      },
    ],
  });
}

function renderInventoryTab(container) {
  container.innerHTML = `
    <div class="card-header">
      ${authStore.hasPermission('products:manage') ? '<button class="btn btn-primary" id="new-product-btn">+ New Product</button>' : '<span></span>'}
    </div>
    <div id="products-table"></div>
  `;

  createDataTable(document.getElementById('products-table'), {
    columns: [
      { label: 'Name', key: 'name' },
      { label: 'SKU', key: 'sku' },
      { label: 'Price', render: (p) => `${p.price} ${p.currency}` },
      { label: 'Stock', render: (p) => p.isLowStock ? rawHtml(`<span class="badge badge-warning">${p.stockQuantity}</span>`) : p.stockQuantity },
    ],
    fetchPage: (params) => api.get('/products', params),
    rowActions: authStore.hasPermission('products:manage') ? (product) => [
      { label: 'Edit', onClick: (row, reload) => openProductModal(row, reload) },
      {
        label: 'Receive Stock',
        onClick: async (row, reload) => {
          const qty = prompt('How many units received?');
          if (!qty || Number.isNaN(Number(qty))) return;
          try {
            await api.post(`/products/${row.id}/receive-stock`, { quantity: Number(qty) });
            toastSuccess('Stock updated.');
            reload();
          } catch (error) {
            toastError(error.message || 'Failed to receive stock.');
          }
        },
      },
    ] : null,
  });

  document.getElementById('new-product-btn')?.addEventListener('click', () => openProductModal(null, () => renderInventoryTab(container)));
}

async function renderPosTab(container) {
  const products = await api.get('/products', { pageSize: 200 });
  const cart = [];

  container.innerHTML = `
    <div class="grid-2">
      <div>
        <div class="form-field" style="margin-bottom: var(--spacing-4);">
          <label for="pos-product">Add Product</label>
          <select id="pos-product">
            ${products.items.map((p) => `<option value="${p.id}" data-price="${p.price}" data-name="${escapeHtml(p.name)}">${escapeHtml(p.name)} — ${p.price} ${p.currency}</option>`).join('')}
          </select>
        </div>
        <button class="btn btn-secondary" id="add-to-cart-btn">Add to Cart</button>
      </div>
      <div>
        <h3>Cart</h3>
        <div id="cart-list"></div>
        <div style="margin-top: var(--spacing-4); font-weight:700;" id="cart-total"></div>
        <button class="btn btn-primary" id="checkout-btn" style="margin-top: var(--spacing-3);">Checkout (Cash)</button>
      </div>
    </div>
  `;

  function renderCart() {
    const cartList = document.getElementById('cart-list');
    cartList.innerHTML = cart.length
      ? cart.map((item, idx) => `<div style="display:flex; justify-content:space-between; padding:6px 0;">${escapeHtml(item.name)} × ${item.quantity} <button data-idx="${idx}" class="btn btn-sm btn-secondary remove-cart-item">Remove</button></div>`).join('')
      : '<p class="text-muted">Cart is empty.</p>';

    const total = cart.reduce((sum, item) => sum + item.price * item.quantity, 0);
    document.getElementById('cart-total').textContent = `Total: ${total.toFixed(2)}`;

    cartList.querySelectorAll('.remove-cart-item').forEach((btn) => {
      btn.addEventListener('click', () => { cart.splice(Number(btn.dataset.idx), 1); renderCart(); });
    });
  }

  document.getElementById('add-to-cart-btn').addEventListener('click', () => {
    const select = document.getElementById('pos-product');
    const option = select.selectedOptions[0];
    if (!option) return;
    const existing = cart.find((item) => item.productId === option.value);
    if (existing) existing.quantity += 1;
    else cart.push({ productId: option.value, name: option.dataset.name, price: Number(option.dataset.price), quantity: 1 });
    renderCart();
  });

  document.getElementById('checkout-btn').addEventListener('click', async () => {
    if (!cart.length) return;
    const branches = await api.get('/branches');
    try {
      await api.post('/sales', {
        branchId: branches[0]?.id,
        memberId: null,
        paymentMethod: 0,
        lines: cart.map((item) => ({ productId: item.productId, quantity: item.quantity })),
      });
      toastSuccess('Sale completed.');
      cart.length = 0;
      renderCart();
    } catch (error) {
      toastError(error.message || 'Checkout failed.');
    }
  });

  renderCart();
}

export function render(container) {
  container.innerHTML = `
    <div class="card">
      <h2>Products & POS</h2>
      <div class="tabs">
        <div class="tab active" data-tab="inventory">Inventory</div>
        ${authStore.hasPermission('pos:sell') ? '<div class="tab" data-tab="pos">Point of Sale</div>' : ''}
      </div>
      <div id="tab-content"></div>
    </div>
  `;

  const tabContent = document.getElementById('tab-content');
  renderInventoryTab(tabContent);

  container.querySelectorAll('.tab').forEach((tabEl) => {
    tabEl.addEventListener('click', () => {
      container.querySelectorAll('.tab').forEach((t) => t.classList.remove('active'));
      tabEl.classList.add('active');
      if (tabEl.dataset.tab === 'inventory') renderInventoryTab(tabContent);
      else renderPosTab(tabContent);
    });
  });
}
