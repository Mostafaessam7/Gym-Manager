using GymManager.Application.Abstractions;
using GymManager.SharedKernel.Cqrs;
using Microsoft.EntityFrameworkCore;

namespace GymManager.Application.Reports;

public sealed record SalesReportRow(DateTimeOffset SoldOnUtc, string? MemberName, decimal TotalAmount, string Currency, string Status);

public sealed record SalesReportQuery(Guid? BranchId, DateOnly From, DateOnly To) : IQuery<IReadOnlyList<SalesReportRow>>;

public sealed class SalesReportQueryHandler(IApplicationReadDb readDb) : IQueryHandler<SalesReportQuery, IReadOnlyList<SalesReportRow>>
{
    public async Task<IReadOnlyList<SalesReportRow>> Handle(SalesReportQuery query, CancellationToken cancellationToken)
    {
        var from = query.From.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var to = query.To.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);

        var sales = readDb.Sales.Include(s => s.Lines).Where(s => s.SoldOnUtc >= from && s.SoldOnUtc <= to);
        if (query.BranchId.HasValue)
            sales = sales.Where(s => s.BranchId == query.BranchId);

        var saleList = await sales.OrderByDescending(s => s.SoldOnUtc).ToListAsync(cancellationToken);

        var memberIds = saleList.Where(s => s.MemberId.HasValue).Select(s => s.MemberId!.Value).Distinct().ToArray();
        var memberNames = await readDb.Members.Where(m => memberIds.Contains(m.Id))
            .ToDictionaryAsync(m => m.Id, m => $"{m.FirstName} {m.LastName}", cancellationToken);

        return saleList
            .Select(s => new SalesReportRow(
                s.SoldOnUtc,
                s.MemberId.HasValue ? memberNames.GetValueOrDefault(s.MemberId.Value, "Unknown") : null,
                s.TotalAmount.Amount, s.Currency, s.Status.ToString()))
            .ToList();
    }
}
