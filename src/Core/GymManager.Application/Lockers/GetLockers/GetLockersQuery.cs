using GymManager.Application.Lockers.Contracts;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Pagination;

namespace GymManager.Application.Lockers.GetLockers;

public sealed record GetLockersQuery(PaginationParameters Pagination, Guid? BranchId, string? Status) : IQuery<PagedList<LockerResponse>>;
