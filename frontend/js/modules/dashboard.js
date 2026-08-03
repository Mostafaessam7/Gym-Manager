import { api } from '../api/apiClient.js';
import { toastError } from '../components/toast.js';
import { escapeHtml } from '../utils/html.js';

function money(amount, currency) {
  return `${Number(amount ?? 0).toFixed(2)} ${currency || ''}`.trim();
}

function renderBarChart(rows) {
  if (!rows.length) return '<p class="text-muted">No revenue recorded in the last 30 days.</p>';

  const max = Math.max(...rows.map((r) => r.amount), 1);
  const bars = rows.map((r) => {
    const heightPct = Math.max(2, Math.round((r.amount / max) * 100));
    return `<div class="bar-chart__bar" title="${r.date}: ${r.amount.toFixed(2)}"><span style="height:${heightPct}%"></span></div>`;
  }).join('');

  return `<div class="bar-chart">${bars}</div>`;
}

export async function render(container) {
  container.innerHTML = '<div class="spinner"></div>';

  try {
    const summary = await api.get('/dashboard/summary');

    container.innerHTML = `
      <div class="stats-grid">
        <div class="stat-card">
          <div class="stat-card__label">Today's Revenue</div>
          <div class="stat-card__value">${money(summary.todaysRevenue, summary.currency)}</div>
        </div>
        <div class="stat-card">
          <div class="stat-card__label">Monthly Revenue</div>
          <div class="stat-card__value">${money(summary.monthlyRevenue, summary.currency)}</div>
        </div>
        <div class="stat-card">
          <div class="stat-card__label">Active Members</div>
          <div class="stat-card__value">${summary.activeMembers}</div>
        </div>
        <div class="stat-card">
          <div class="stat-card__label">Attendance Today</div>
          <div class="stat-card__value">${summary.attendanceToday}</div>
        </div>
        <div class="stat-card">
          <div class="stat-card__label">New Members (Month)</div>
          <div class="stat-card__value">${summary.newMembersThisMonth}</div>
        </div>
        <div class="stat-card">
          <div class="stat-card__label">Expiring Soon (7d)</div>
          <div class="stat-card__value">${summary.membersExpiringSoon}</div>
        </div>
      </div>

      <div class="grid-2">
        <div class="card">
          <div class="card-header"><h3>Revenue — Last 30 Days</h3></div>
          ${renderBarChart(summary.revenueLast30Days.map((r) => ({ date: r.date, amount: r.amount })))}
        </div>
        <div class="card">
          <div class="card-header"><h3>Inventory Alerts</h3></div>
          ${summary.inventoryAlerts.length
            ? `<ul>${summary.inventoryAlerts.map((a) => `<li>${escapeHtml(a)}</li>`).join('')}</ul>`
            : '<p class="text-muted">No low-stock products.</p>'}
        </div>
      </div>

      <div class="grid-2" style="margin-top: var(--spacing-5);">
        <div class="card">
          <div class="card-header"><h3>Recent Payments</h3></div>
          ${summary.recentPayments.length
            ? `<div class="data-table-wrap"><table class="data-table"><thead><tr><th>Amount</th><th>Method</th><th>Date</th></tr></thead><tbody>
                ${summary.recentPayments.map((p) => `<tr><td>${money(p.amount, p.currency)}</td><td>${p.method}</td><td>${new Date(p.createdOnUtc).toLocaleString()}</td></tr>`).join('')}
              </tbody></table></div>`
            : '<p class="text-muted">No recent payments.</p>'}
        </div>
        <div class="card">
          <div class="card-header"><h3>Recent Check-ins</h3></div>
          ${summary.recentCheckIns.length
            ? `<div class="data-table-wrap"><table class="data-table"><thead><tr><th>Member</th><th>Method</th><th>Time</th></tr></thead><tbody>
                ${summary.recentCheckIns.map((c) => `<tr><td>${escapeHtml(c.memberName)}</td><td>${escapeHtml(c.method)}</td><td>${new Date(c.checkInUtc).toLocaleString()}</td></tr>`).join('')}
              </tbody></table></div>`
            : '<p class="text-muted">No check-ins yet today.</p>'}
        </div>
      </div>

      <div class="card" style="margin-top: var(--spacing-5);">
        <div class="card-header"><h3>Top Trainers (This Month)</h3></div>
        ${summary.topTrainers.length
          ? `<div class="data-table-wrap"><table class="data-table"><thead><tr><th>Trainer</th><th>Sessions</th><th>Bookings</th></tr></thead><tbody>
              ${summary.topTrainers.map((t) => `<tr><td>${escapeHtml(t.trainerName)}</td><td>${t.sessionCount}</td><td>${t.bookingCount}</td></tr>`).join('')}
            </tbody></table></div>`
          : '<p class="text-muted">No class sessions this month.</p>'}
      </div>
    `;
  } catch (error) {
    toastError(error.message || 'Failed to load dashboard.');
    container.innerHTML = '<div class="empty-state">Failed to load dashboard.</div>';
  }
}
