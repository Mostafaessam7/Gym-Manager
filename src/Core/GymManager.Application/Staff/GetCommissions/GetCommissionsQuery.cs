using GymManager.Application.Staff.Contracts;
using GymManager.Domain.Staff;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Pagination;

namespace GymManager.Application.Staff.GetCommissions;

public sealed record GetCommissionsQuery(PaginationParameters Pagination, Guid? UserId, CommissionStatus? Status)
    : IQuery<PagedList<CommissionResponse>>;
