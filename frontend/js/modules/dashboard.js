import { api } from '../api/apiClient.js';
import { toastError } from '../components/toast.js';
import { escapeHtml } from '../utils/html.js';
import { t, tStatus } from '../i18n/index.js';

function money(amount, currency) {
  return `${Number(amount ?? 0).toFixed(2)} ${currency || ''}`.trim();
}

function renderBarChart(rows) {
  if (!rows.length) return `<p class="text-muted">${t('dashboard.noClassSessions')}</p>`;

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
          <div class="stat-card__label">${t('dashboard.todaysRevenue')}</div>
          <div class="stat-card__value">${money(summary.todaysRevenue, summary.currency)}</div>
        </div>
        <div class="stat-card">
          <div class="stat-card__label">${t('dashboard.monthlyRevenue')}</div>
          <div class="stat-card__value">${money(summary.monthlyRevenue, summary.currency)}</div>
        </div>
        <div class="stat-card">
          <div class="stat-card__label">${t('dashboard.activeMembers')}</div>
          <div class="stat-card__value">${summary.activeMembers}</div>
        </div>
        <div class="stat-card">
          <div class="stat-card__label">${t('dashboard.attendanceToday')}</div>
          <div class="stat-card__value">${summary.attendanceToday}</div>
        </div>
        <div class="stat-card">
          <div class="stat-card__label">${t('dashboard.newMembersMonth')}</div>
          <div class="stat-card__value">${summary.newMembersThisMonth}</div>
        </div>
        <div class="stat-card">
          <div class="stat-card__label">${t('dashboard.expiringSoon')}</div>
          <div class="stat-card__value">${summary.membersExpiringSoon}</div>
        </div>
      </div>

      <div class="grid-2">
        <div class="card">
          <div class="card-header"><h3>${t('dashboard.revenueLast30')}</h3></div>
          ${renderBarChart(summary.revenueLast30Days.map((r) => ({ date: r.date, amount: r.amount })))}
        </div>
        <div class="card">
          <div class="card-header"><h3>${t('dashboard.inventoryAlerts')}</h3></div>
          ${summary.inventoryAlerts.length
            ? `<ul>${summary.inventoryAlerts.map((a) => `<li>${escapeHtml(a)}</li>`).join('')}</ul>`
            : `<p class="text-muted">${t('dashboard.noLowStock')}</p>`}
        </div>
      </div>

      <div class="grid-2" style="margin-top: var(--spacing-5);">
        <div class="card">
          <div class="card-header"><h3>${t('dashboard.recentPayments')}</h3></div>
          ${summary.recentPayments.length
            ? `<div class="data-table-wrap"><table class="data-table"><thead><tr><th>${t('dashboard.amount')}</th><th>${t('dashboard.method')}</th><th>${t('dashboard.date')}</th></tr></thead><tbody>
                ${summary.recentPayments.map((p) => `<tr><td>${money(p.amount, p.currency)}</td><td>${tStatus(p.method)}</td><td>${new Date(p.createdOnUtc).toLocaleString()}</td></tr>`).join('')}
              </tbody></table></div>`
            : `<p class="text-muted">${t('dashboard.noRecentPayments')}</p>`}
        </div>
        <div class="card">
          <div class="card-header"><h3>${t('dashboard.recentCheckIns')}</h3></div>
          ${summary.recentCheckIns.length
            ? `<div class="data-table-wrap"><table class="data-table"><thead><tr><th>${t('dashboard.member')}</th><th>${t('dashboard.method')}</th><th>${t('dashboard.time')}</th></tr></thead><tbody>
                ${summary.recentCheckIns.map((c) => `<tr><td>${escapeHtml(c.memberName)}</td><td>${escapeHtml(c.method)}</td><td>${new Date(c.checkInUtc).toLocaleString()}</td></tr>`).join('')}
              </tbody></table></div>`
            : `<p class="text-muted">${t('dashboard.noRecentCheckIns')}</p>`}
        </div>
      </div>

      <div class="card" style="margin-top: var(--spacing-5);">
        <div class="card-header"><h3>${t('dashboard.topTrainers')}</h3></div>
        ${summary.topTrainers.length
          ? `<div class="data-table-wrap"><table class="data-table"><thead><tr><th>${t('trainers.title')}</th><th>${t('dashboard.sessions')}</th><th>${t('dashboard.bookings')}</th></tr></thead><tbody>
              ${summary.topTrainers.map((t2) => `<tr><td>${escapeHtml(t2.trainerName)}</td><td>${t2.sessionCount}</td><td>${t2.bookingCount}</td></tr>`).join('')}
            </tbody></table></div>`
          : `<p class="text-muted">${t('dashboard.noClassSessions')}</p>`}
      </div>
    `;
  } catch (error) {
    toastError(error.message || t('dashboard.failedToLoad'));
    container.innerHTML = `<div class="empty-state">${t('dashboard.failedToLoad')}</div>`;
  }
}
