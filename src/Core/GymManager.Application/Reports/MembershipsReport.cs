using GymManager.Application.Abstractions;
using GymManager.SharedKernel.Cqrs;
using Microsoft.EntityFrameworkCore;

namespace GymManager.Application.Reports;

public sealed record MembershipReportRow(
    string MemberName, string PlanName, DateOnly StartDate, DateOnly EndDate, string Status, decimal PricePaid, string Currency);

public sealed record MembershipsReportQuery(Guid? BranchId, string? Status) : IQuery<IReadOnlyList<MembershipReportRow>>;

public sealed class MembershipsReportQueryHandler(IApplicationReadDb readDb) : IQueryHandler<MembershipsReportQuery, IReadOnlyList<MembershipReportRow>>
{
    public async Task<IReadOnlyList<MembershipReportRow>> Handle(MembershipsReportQuery query, CancellationToken cancellationToken)
    {
        var memberships = readDb.Memberships.AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Status) &&
            Enum.TryParse<Domain.Memberships.MembershipStatus>(query.Status, true, out var status))
            memberships = memberships.Where(m => m.Status == status);

        var rows = await memberships
            .OrderByDescending(m => m.StartDate)
            .Select(m => new
            {
                m.MemberId, m.PlanNameSnapshot, m.StartDate, m.EndDate, m.Status,
                m.PricePaid.Amount, m.PricePaid.Currency,
            })
            .ToListAsync(cancellationToken);

        var memberIds = rows.Select(r => r.MemberId).Distinct().ToArray();
        var membersQuery = readDb.Members.Where(m => memberIds.Contains(m.Id));
        if (query.BranchId.HasValue)
            membersQuery = membersQuery.Where(m => m.BranchId == query.BranchId);

        var memberNames = await membersQuery.ToDictionaryAsync(m => m.Id, m => $"{m.FirstName} {m.LastName}", cancellationToken);

        return rows
            .Where(r => memberNames.ContainsKey(r.MemberId))
            .Select(r => new MembershipReportRow(
                memberNames[r.MemberId], r.PlanNameSnapshot, r.StartDate, r.EndDate, r.Status.ToString(), r.Amount, r.Currency))
            .ToList();
    }
}
