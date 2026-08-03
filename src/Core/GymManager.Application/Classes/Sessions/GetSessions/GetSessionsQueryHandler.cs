using GymManager.Application.Abstractions;
using GymManager.Application.Classes.Contracts;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Pagination;
using Microsoft.EntityFrameworkCore;

namespace GymManager.Application.Classes.Sessions.GetSessions;

public sealed class GetSessionsQueryHandler(IApplicationReadDb readDb, IBranchAccessGuard branchAccessGuard)
    : IQueryHandler<GetSessionsQuery, PagedList<ClassSessionResponse>>
{
    public async Task<PagedList<ClassSessionResponse>> Handle(GetSessionsQuery query, CancellationToken cancellationToken)
    {
        var pagination = query.Pagination;
        var sessions = readDb.ClassSessions.AsQueryable();

        var branchId = branchAccessGuard.ResolveFilter(query.BranchId);
        if (branchId.HasValue)
            sessions = sessions.Where(s => s.BranchId == branchId);

        if (query.TrainerId.HasValue)
            sessions = sessions.Where(s => s.TrainerId == query.TrainerId);

        if (query.GymClassId.HasValue)
            sessions = sessions.Where(s => s.GymClassId == query.GymClassId);

        if (query.From.HasValue)
            sessions = sessions.Where(s => s.StartUtc >= query.From);

        if (query.To.HasValue)
            sessions = sessions.Where(s => s.StartUtc <= query.To);

        sessions = pagination.SortDescending ? sessions.OrderByDescending(s => s.StartUtc) : sessions.OrderBy(s => s.StartUtc);

        var totalCount = await sessions.CountAsync(cancellationToken);

        var items = await sessions
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedList<ClassSessionResponse>(items.Select(s => s.ToResponse()).ToList(), pagination.PageNumber, pagination.PageSize, totalCount);
    }
}
