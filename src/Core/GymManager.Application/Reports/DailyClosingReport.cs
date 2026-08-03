using GymManager.Application.Abstractions;
using GymManager.Domain.Payments;
using GymManager.SharedKernel.Cqrs;
using Microsoft.EntityFrameworkCore;

namespace GymManager.Application.Reports;

public sealed record DailyClosingRow(
    DateOnly Date, decimal CashTotal, decimal CardTotal, decimal OtherTotal, decimal TotalRevenue, decimal TotalExpenses,
    int SalesCount, int AttendanceCount, string Currency);

public sealed record DailyClosingReportQuery(Guid? BranchId, DateOnly Date) : IQuery<DailyClosingRow>;

public sealed class DailyClosingReportQueryHandler(IApplicationReadDb readDb) : IQueryHandler<DailyClosingReportQuery, DailyClosingRow>
{
    public async Task<DailyClosingRow> Handle(DailyClosingReportQuery query, CancellationToken cancellationToken)
    {
        var dayStart = query.Date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var dayEnd = query.Date.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);

        var payments = readDb.Payments.Where(p => p.Status == PaymentStatus.Completed && p.CreatedOnUtc >= dayStart && p.CreatedOnUtc <= dayEnd);
        if (query.BranchId.HasValue)
            payments = payments.Where(p => p.BranchId == query.BranchId);

        var paymentData = await payments.Select(p => new { p.Method, p.Amount.Amount, p.Amount.Currency }).ToListAsync(cancellationToken);

        var expenses = readDb.Expenses.Where(e => e.ExpenseDate == query.Date);
        if (query.BranchId.HasValue)
            expenses = expenses.Where(e => e.BranchId == query.BranchId);
        var totalExpenses = await expenses.SumAsync(e => e.Amount.Amount, cancellationToken);

        var salesQuery = readDb.Sales.Where(s => s.SoldOnUtc >= dayStart && s.SoldOnUtc <= dayEnd);
        if (query.BranchId.HasValue)
            salesQuery = salesQuery.Where(s => s.BranchId == query.BranchId);
        var salesCount = await salesQuery.CountAsync(cancellationToken);

        var attendanceQuery = readDb.AttendanceRecords.Where(a => a.CheckInUtc >= dayStart && a.CheckInUtc <= dayEnd);
        if (query.BranchId.HasValue)
            attendanceQuery = attendanceQuery.Where(a => a.BranchId == query.BranchId);
        var attendanceCount = await attendanceQuery.CountAsync(cancellationToken);

        var cashTotal = paymentData.Where(p => p.Method == PaymentMethod.Cash).Sum(p => p.Amount);
        var cardTotal = paymentData.Where(p => p.Method == PaymentMethod.Card).Sum(p => p.Amount);
        var otherTotal = paymentData.Where(p => p.Method is not PaymentMethod.Cash and not PaymentMethod.Card).Sum(p => p.Amount);
        var currency = paymentData.Count > 0 ? paymentData[0].Currency : "USD";

        return new DailyClosingRow(
            query.Date, cashTotal, cardTotal, otherTotal, cashTotal + cardTotal + otherTotal, totalExpenses,
            salesCount, attendanceCount, currency);
    }
}
