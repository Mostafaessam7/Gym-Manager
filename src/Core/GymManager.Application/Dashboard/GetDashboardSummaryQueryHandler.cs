using GymManager.Application.Abstractions;
using GymManager.Domain.Memberships;
using GymManager.Domain.Payments;
using GymManager.SharedKernel.Cqrs;
using Microsoft.EntityFrameworkCore;

namespace GymManager.Application.Dashboard;

public sealed class GetDashboardSummaryQueryHandler(
    IApplicationReadDb readDb, IDateTimeProvider dateTimeProvider, IBranchAccessGuard branchAccessGuard)
    : IQueryHandler<GetDashboardSummaryQuery, DashboardSummaryResponse>
{
    public async Task<DashboardSummaryResponse> Handle(GetDashboardSummaryQuery query, CancellationToken cancellationToken)
    {
        var branchId = branchAccessGuard.ResolveFilter(query.BranchId);

        var today = dateTimeProvider.TodayUtc;
        var todayStart = today.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var monthStart = new DateOnly(today.Year, today.Month, 1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var thirtyDaysAgo = today.AddDays(-30);

        var payments = readDb.Payments.Where(p => p.Status == PaymentStatus.Completed);
        if (branchId.HasValue)
            payments = payments.Where(p => p.BranchId == branchId);

        var completedPayments = await payments
            .Where(p => p.CreatedOnUtc >= monthStart)
            .Select(p => new { p.CreatedOnUtc, p.Amount.Amount, p.Amount.Currency })
            .ToListAsync(cancellationToken);

        var currency = completedPayments.Count > 0 ? completedPayments[0].Currency : "USD";
        var todaysRevenue = completedPayments.Where(p => p.CreatedOnUtc >= todayStart).Sum(p => p.Amount);
        var monthlyRevenue = completedPayments.Sum(p => p.Amount);

        var revenueLast30Days = completedPayments
            .Where(p => DateOnly.FromDateTime(p.CreatedOnUtc.UtcDateTime) >= thirtyDaysAgo)
            .GroupBy(p => DateOnly.FromDateTime(p.CreatedOnUtc.UtcDateTime))
            .Select(g => new RevenueByDayRow(g.Key, g.Sum(x => x.Amount)))
            .OrderBy(r => r.Date)
            .ToList();

        var members = readDb.Members.AsQueryable();
        if (branchId.HasValue)
            members = members.Where(m => m.BranchId == branchId);

        var newMembersThisMonth = await members.CountAsync(m => m.JoinedOnUtc >= monthStart, cancellationToken);

        var memberships = readDb.Memberships.AsQueryable();
        if (branchId.HasValue)
        {
            var branchMemberIds = readDb.Members.Where(m => m.BranchId == branchId).Select(m => m.Id);
            memberships = memberships.Where(m => branchMemberIds.Contains(m.MemberId));
        }

        var activeMembers = await memberships.CountAsync(m => m.Status == MembershipStatus.Active && m.EndDate >= today, cancellationToken);
        var expiredMemberships = await memberships.CountAsync(m => m.Status == MembershipStatus.Expired, cancellationToken);
        var membersExpiringSoon = await memberships.CountAsync(
            m => m.Status == MembershipStatus.Active && m.EndDate >= today && m.EndDate <= today.AddDays(7), cancellationToken);

        var attendanceQuery = readDb.AttendanceRecords.Where(a => a.CheckInUtc >= todayStart);
        if (branchId.HasValue)
            attendanceQuery = attendanceQuery.Where(a => a.BranchId == branchId);
        var attendanceToday = await attendanceQuery.CountAsync(cancellationToken);

        var recentCheckInRecords = await attendanceQuery.OrderByDescending(a => a.CheckInUtc).Take(10)
            .Select(a => new { a.MemberId, a.CheckInUtc, a.Method })
            .ToListAsync(cancellationToken);
        var checkInMemberIds = recentCheckInRecords.Select(r => r.MemberId).Distinct().ToArray();
        var memberNames = await readDb.Members.Where(m => checkInMemberIds.Contains(m.Id))
            .ToDictionaryAsync(m => m.Id, m => $"{m.FirstName} {m.LastName}", cancellationToken);
        var recentCheckIns = recentCheckInRecords
            .Select(r => new RecentCheckInRow(r.MemberId, memberNames.GetValueOrDefault(r.MemberId, "Unknown"), r.CheckInUtc, r.Method.ToString()))
            .ToList();

        var recentPaymentsQuery = readDb.Payments.Where(p => p.Status == PaymentStatus.Completed);
        if (branchId.HasValue)
            recentPaymentsQuery = recentPaymentsQuery.Where(p => p.BranchId == branchId);
        var recentPayments = await recentPaymentsQuery
            .OrderByDescending(p => p.CreatedOnUtc).Take(10)
            .Select(p => new RecentPaymentRow(p.Id, p.MemberId, p.Amount.Amount, p.Amount.Currency, p.Method.ToString(), p.CreatedOnUtc))
            .ToListAsync(cancellationToken);

        var sessionsQuery = readDb.ClassSessions.Where(s => s.StartUtc >= monthStart);
        if (branchId.HasValue)
            sessionsQuery = sessionsQuery.Where(s => s.BranchId == branchId);
        var sessions = await sessionsQuery.Select(s => new { s.TrainerId, BookingCount = s.Bookings.Count(b => b.Status != Domain.Classes.BookingStatus.Cancelled) }).ToListAsync(cancellationToken);
        var trainerIds = sessions.Select(s => s.TrainerId).Distinct().ToArray();
        var trainerNames = await readDb.Trainers.Where(t => trainerIds.Contains(t.Id))
            .ToDictionaryAsync(t => t.Id, t => $"{t.FirstName} {t.LastName}", cancellationToken);
        var topTrainers = sessions.GroupBy(s => s.TrainerId)
            .Select(g => new TopTrainerRow(g.Key, trainerNames.GetValueOrDefault(g.Key, "Unknown"), g.Count(), g.Sum(x => x.BookingCount)))
            .OrderByDescending(t => t.BookingCount)
            .Take(5)
            .ToList();

        var salesQuery = readDb.Sales.Include(s => s.Lines).Where(s => s.SoldOnUtc >= monthStart);
        if (branchId.HasValue)
            salesQuery = salesQuery.Where(s => s.BranchId == branchId);
        var sales = await salesQuery.ToListAsync(cancellationToken);
        var topSellingProducts = sales.SelectMany(s => s.Lines)
            .GroupBy(l => l.ProductId)
            .Select(g => new TopProductRow(g.Key, g.First().ProductNameSnapshot, g.Sum(x => x.Quantity), g.Sum(x => x.LineTotal.Amount)))
            .OrderByDescending(p => p.QuantitySold)
            .Take(5)
            .ToList();

        var productsQuery = readDb.Products.Where(p => p.IsActive && p.StockQuantity <= p.ReorderThreshold);
        if (branchId.HasValue)
            productsQuery = productsQuery.Where(p => p.BranchId == branchId);
        var inventoryAlerts = await productsQuery
            .Select(p => $"{p.Name} ({p.StockQuantity} remaining, reorder at {p.ReorderThreshold})")
            .ToListAsync(cancellationToken);

        return new DashboardSummaryResponse(
            todaysRevenue, monthlyRevenue, currency, activeMembers, expiredMemberships, membersExpiringSoon,
            attendanceToday, newMembersThisMonth, topTrainers, topSellingProducts, inventoryAlerts, recentPayments,
            recentCheckIns, revenueLast30Days);
    }
}
