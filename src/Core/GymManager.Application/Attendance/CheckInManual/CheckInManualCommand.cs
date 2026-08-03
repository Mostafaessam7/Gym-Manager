using GymManager.Application.Attendance.Contracts;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;

namespace GymManager.Application.Attendance.CheckInManual;

public sealed record CheckInManualCommand(Guid MemberId) : ICommand<Result<AttendanceRecordResponse>>;
