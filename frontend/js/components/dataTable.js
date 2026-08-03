import { toastError } from './toast.js';
import { RawHtml } from '../utils/html.js';

// A reusable, paginated, searchable, sortable table bound to a server-side `fetchPage` function.
// fetchPage receives { pageNumber, pageSize, searchTerm, sortBy, sortDescending, ...extraParams }
// and must resolve to either a PagedList shape ({ items, totalCount, pageNumber, pageSize })
// or a plain array (treated as a single, unpaginated page).
export function createDataTable(container, options) {
  const {
    columns,
    fetchPage,
    pageSize = 10,
    searchable = true,
    getExtraParams = () => ({}),
    emptyMessage = 'No records found.',
    rowActions = null,
  } = options;

  const state = {
    pageNumber: 1,
    pageSize,
    searchTerm: '',
    sortBy: null,
    sortDescending: false,
  };

  container.innerHTML = '';

  const toolbar = document.createElement('div');
  toolbar.className = 'table-toolbar';

  if (searchable) {
    const searchWrap = document.createElement('div');
    searchWrap.className = 'search-input-wrap';
    const searchInput = document.createElement('input');
    searchInput.type = 'search';
    searchInput.placeholder = 'Search…';
    let debounceHandle;
    searchInput.addEventListener('input', () => {
      clearTimeout(debounceHandle);
      debounceHandle = setTimeout(() => {
        state.searchTerm = searchInput.value;
        state.pageNumber = 1;
        load();
      }, 300);
    });
    searchWrap.appendChild(searchInput);
    toolbar.appendChild(searchWrap);
  }

  const toolbarExtra = document.createElement('div');
  toolbar.appendChild(toolbarExtra);

  const tableWrap = document.createElement('div');
  tableWrap.className = 'data-table-wrap';

  const table = document.createElement('table');
  table.className = 'data-table';

  const thead = document.createElement('thead');
  const headerRow = document.createElement('tr');
  columns.forEach((col) => {
    const th = document.createElement('th');
    th.textContent = col.label;
    if (col.sortKey) {
      th.addEventListener('click', () => {
        if (state.sortBy === col.sortKey) {
          state.sortDescending = !state.sortDescending;
        } else {
          state.sortBy = col.sortKey;
          state.sortDescending = false;
        }
        load();
      });
    }
    headerRow.appendChild(th);
  });
  if (rowActions) headerRow.appendChild(document.createElement('th'));
  thead.appendChild(headerRow);

  const tbody = document.createElement('tbody');
  table.appendChild(thead);
  table.appendChild(tbody);
  tableWrap.appendChild(table);

  const pagination = document.createElement('div');
  pagination.className = 'table-pagination';

  container.appendChild(toolbar);
  container.appendChild(tableWrap);
  container.appendChild(pagination);

  async function load() {
    tbody.innerHTML = `<tr><td colspan="${columns.length + (rowActions ? 1 : 0)}"><div class="spinner"></div></td></tr>`;

    try {
      const params = {
        pageNumber: state.pageNumber,
        pageSize: state.pageSize,
        searchTerm: state.searchTerm || undefined,
        sortBy: state.sortBy || undefined,
        sortDescending: state.sortBy ? state.sortDescending : undefined,
        ...getExtraParams(),
      };

      const result = await fetchPage(params);
      const isPaged = result && !Array.isArray(result) && 'items' in result;
      const items = isPaged ? result.items : result || [];
      const totalCount = isPaged ? result.totalCount : items.length;

      renderRows(items);
      renderPagination(totalCount, isPaged);
    } catch (error) {
      tbody.innerHTML = `<tr><td colspan="${columns.length + (rowActions ? 1 : 0)}"><div class="empty-state">Failed to load data.</div></td></tr>`;
      toastError(error.message || 'Failed to load data.');
    }
  }

  function renderRows(items) {
    tbody.innerHTML = '';

    if (!items.length) {
      const emptyRow = document.createElement('tr');
      const cell = document.createElement('td');
      cell.colSpan = columns.length + (rowActions ? 1 : 0);
      cell.innerHTML = `<div class="empty-state">${emptyMessage}</div>`;
      emptyRow.appendChild(cell);
      tbody.appendChild(emptyRow);
      return;
    }

    items.forEach((row) => {
      const tr = document.createElement('tr');
      columns.forEach((col) => {
        const td = document.createElement('td');
        const value = col.render ? col.render(row) : row[col.key];
        if (value instanceof Node) td.appendChild(value);
        else if (value instanceof RawHtml) td.innerHTML = value.html;
        else td.textContent = value ?? '';
        tr.appendChild(td);
      });

      if (rowActions) {
        const actionsCell = document.createElement('td');
        rowActions(row).forEach((action) => {
          const btn = document.createElement('button');
          btn.className = `btn btn-sm ${action.className || 'btn-secondary'}`;
          btn.textContent = action.label;
          btn.style.marginRight = '6px';
          btn.addEventListener('click', () => action.onClick(row, load));
          actionsCell.appendChild(btn);
        });
        tr.appendChild(actionsCell);
      }

      tbody.appendChild(tr);
    });
  }

  function renderPagination(totalCount, isPaged) {
    if (!isPaged) {
      pagination.innerHTML = `<span>${totalCount} record${totalCount === 1 ? '' : 's'}</span>`;
      return;
    }

    const totalPages = Math.max(1, Math.ceil(totalCount / state.pageSize));
    pagination.innerHTML = '';

    const summary = document.createElement('span');
    summary.textContent = `Page ${state.pageNumber} of ${totalPages} · ${totalCount} total`;

    const controls = document.createElement('div');
    controls.className = 'table-pagination__controls';

    const prevBtn = document.createElement('button');
    prevBtn.className = 'btn btn-sm btn-secondary';
    prevBtn.textContent = 'Previous';
    prevBtn.disabled = state.pageNumber <= 1;
    prevBtn.addEventListener('click', () => {
      state.pageNumber -= 1;
      load();
    });

    const nextBtn = document.createElement('button');
    nextBtn.className = 'btn btn-sm btn-secondary';
    nextBtn.textContent = 'Next';
    nextBtn.disabled = state.pageNumber >= totalPages;
    nextBtn.addEventListener('click', () => {
      state.pageNumber += 1;
      load();
    });

    controls.appendChild(prevBtn);
    controls.appendChild(nextBtn);

    pagination.appendChild(summary);
    pagination.appendChild(controls);
  }

  load();

  return { refresh: load, toolbarExtra, state };
}
