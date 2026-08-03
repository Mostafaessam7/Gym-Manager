using GymManager.Application.Attendance.Contracts;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Pagination;

namespace GymManager.Application.Attendance.GetAttendanceRecords;

public sealed record GetAttendanceRecordsQuery(
    PaginationParameters Pagination, Guid? BranchId, Guid? MemberId, DateOnly? From, DateOnly? To)
    : IQuery<PagedList<AttendanceRecordResponse>>;
