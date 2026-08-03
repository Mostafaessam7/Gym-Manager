using GymManager.SharedKernel.Cqrs;

namespace GymManager.Application.Staff.ApproveLeave;

public sealed record ApproveLeaveCommand(Guid LeaveRequestId, string? Notes) : ICommand;
