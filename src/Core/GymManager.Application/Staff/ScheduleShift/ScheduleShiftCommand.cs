using GymManager.Application.Staff.Contracts;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;

namespace GymManager.Application.Staff.ScheduleShift;

public sealed record ScheduleShiftCommand(Guid UserId, Guid BranchId, DateTimeOffset StartUtc, DateTimeOffset EndUtc, string? Notes)
    : ICommand<Result<StaffShiftResponse>>;
