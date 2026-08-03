using GymManager.Application.Staff.Contracts;
using GymManager.Domain.Staff;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Pagination;

namespace GymManager.Application.Staff.GetLeaveRequests;

public sealed record GetLeaveRequestsQuery(PaginationParameters Pagination, Guid? UserId, LeaveRequestStatus? Status)
    : IQuery<PagedList<LeaveRequestResponse>>;
