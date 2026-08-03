using GymManager.Application.Staff.Contracts;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Pagination;

namespace GymManager.Application.Staff.GetShifts;

public sealed record GetShiftsQuery(PaginationParameters Pagination, Guid? UserId, Guid? BranchId) : IQuery<PagedList<StaffShiftResponse>>;
