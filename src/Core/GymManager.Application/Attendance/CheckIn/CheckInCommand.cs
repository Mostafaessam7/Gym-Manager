using GymManager.Application.Attendance.Contracts;
using GymManager.Domain.Attendance;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;

namespace GymManager.Application.Attendance.CheckIn;

public sealed record CheckInCommand(string CheckInCode, CheckInMethod Method) : ICommand<Result<AttendanceRecordResponse>>;
