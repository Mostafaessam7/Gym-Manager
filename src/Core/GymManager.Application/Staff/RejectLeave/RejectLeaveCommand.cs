using GymManager.SharedKernel.Cqrs;

namespace GymManager.Application.Staff.RejectLeave;

public sealed record RejectLeaveCommand(Guid LeaveRequestId, string? Notes) : ICommand;
