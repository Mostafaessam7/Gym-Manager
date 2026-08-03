using GymManager.Application.Abstractions;
using GymManager.SharedKernel.Cqrs;
using Microsoft.EntityFrameworkCore;

namespace GymManager.Application.Reports;

public sealed record MemberReportRow(
    string MemberCode, string FullName, string PhoneNumber, string? Email, string Status, DateTimeOffset JoinedOnUtc);

public sealed record MembersReportQuery(Guid? BranchId, DateOnly? From, DateOnly? To) : IQuery<IReadOnlyList<MemberReportRow>>;

public sealed class MembersReportQueryHandler(IApplicationReadDb readDb) : IQueryHandler<MembersReportQuery, IReadOnlyList<MemberReportRow>>
{
    public async Task<IReadOnlyList<MemberReportRow>> Handle(MembersReportQuery query, CancellationToken cancellationToken)
    {
        var members = readDb.Members.AsQueryable();

        if (query.BranchId.HasValue)
            members = members.Where(m => m.BranchId == query.BranchId);

        if (query.From.HasValue)
            members = members.Where(m => m.JoinedOnUtc >= query.From.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc));

        if (query.To.HasValue)
            members = members.Where(m => m.JoinedOnUtc <= query.To.Value.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc));

        return await members
            .OrderBy(m => m.JoinedOnUtc)
            .Select(m => new MemberReportRow(
                m.MemberCode, m.FirstName + " " + m.LastName, m.PhoneNumber, m.Email != null ? m.Email.Value : null,
                m.Status.ToString(), m.JoinedOnUtc))
            .ToListAsync(cancellationToken);
    }
}
