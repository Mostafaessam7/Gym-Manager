using GymManager.SharedKernel.Cqrs;

namespace GymManager.Application.Lockers.ReleaseLocker;

public sealed record ReleaseLockerCommand(Guid LockerId) : ICommand;
