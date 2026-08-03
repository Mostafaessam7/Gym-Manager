using GymManager.Application.Staff.Contracts;
using GymManager.Domain.Staff;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;

namespace GymManager.Application.Staff.RequestLeave;

public sealed record RequestLeaveCommand(Guid UserId, LeaveType Type, DateOnly StartDate, DateOnly EndDate, string? Reason)
    : ICommand<Result<LeaveRequestResponse>>;
