using GymManager.Application.Memberships.Contracts;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Pagination;

namespace GymManager.Application.Memberships.Plans.GetPlans;

public sealed record GetPlansQuery(PaginationParameters Pagination, Guid? BranchId, bool IncludeInactive) : IQuery<PagedList<MembershipPlanResponse>>;
