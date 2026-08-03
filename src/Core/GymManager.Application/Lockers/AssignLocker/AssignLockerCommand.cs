using GymManager.SharedKernel.Cqrs;

namespace GymManager.Application.Lockers.AssignLocker;

public sealed record AssignLockerCommand(Guid LockerId, Guid MemberId) : ICommand;
