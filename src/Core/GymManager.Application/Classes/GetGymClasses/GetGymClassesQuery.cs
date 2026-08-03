using GymManager.Application.Classes.Contracts;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Pagination;

namespace GymManager.Application.Classes.GetGymClasses;

public sealed record GetGymClassesQuery(PaginationParameters Pagination, Guid? BranchId, bool IncludeInactive) : IQuery<PagedList<GymClassResponse>>;
