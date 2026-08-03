using GymManager.Application.Abstractions;
using GymManager.Application.Members.Contracts;
using GymManager.Domain.Members.Errors;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace GymManager.Application.Members.GetMemberTimeline;

public sealed class GetMemberTimelineQueryHandler(IApplicationReadDb readDb)
    : IQueryHandler<GetMemberTimelineQuery, Result<IReadOnlyCollection<MemberTimelineEntryResponse>>>
{
    private const int MaxEntries = 200;

    public async Task<Result<IReadOnlyCollection<MemberTimelineEntryResponse>>> Handle(
        GetMemberTimelineQuery query, CancellationToken cancellationToken)
    {
        var memberExists = await readDb.Members.AnyAsync(m => m.Id == query.MemberId, cancellationToken);
        if (!memberExists)
            return Result.Failure<IReadOnlyCollection<MemberTimelineEntryResponse>>(MemberErrors.NotFound);

        var checkIns = await readDb.AttendanceRecords
            .Where(a => a.MemberId == query.MemberId)
            .Select(a => new MemberTimelineEntryResponse(a.CheckInUtc, "CheckIn", $"Checked in via {a.Method}"))
            .ToListAsync(cancellationToken);

        var payments = await readDb.Payments
            .Where(p => p.MemberId == query.MemberId)
            .Select(p => new MemberTimelineEntryResponse(
                p.CreatedOnUtc, "Payment", $"{p.Status} payment of {p.Amount.Amount} {p.Amount.Currency} via {p.Method}"))
            .ToListAsync(cancellationToken);

        var memberships = await readDb.Memberships
            .Where(m => m.MemberId == query.MemberId)
            .Select(m => new MemberTimelineEntryResponse(
                m.CreatedOnUtc, "Membership", $"{m.PlanNameSnapshot} membership ({m.Status}), {m.StartDate:d} to {m.EndDate:d}"))
            .ToListAsync(cancellationToken);

        var entries = checkIns
            .Concat(payments)
            .Concat(memberships)
            .OrderByDescending(e => e.OccurredOnUtc)
            .Take(MaxEntries)
            .ToArray();

        return Result.Success<IReadOnlyCollection<MemberTimelineEntryResponse>>(entries);
    }
}
