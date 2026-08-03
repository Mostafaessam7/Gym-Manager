using GymManager.Application.Classes.Contracts;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Pagination;

namespace GymManager.Application.Classes.Sessions.GetSessions;

public sealed record GetSessionsQuery(
    PaginationParameters Pagination, Guid? BranchId, Guid? TrainerId, Guid? GymClassId, DateTimeOffset? From, DateTimeOffset? To)
    : IQuery<PagedList<ClassSessionResponse>>;
