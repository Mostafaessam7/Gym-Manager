using GymManager.SharedKernel.Cqrs;

namespace GymManager.Application.Lockers.SetLockerMaintenance;

public sealed record SetLockerMaintenanceCommand(Guid LockerId) : ICommand;
