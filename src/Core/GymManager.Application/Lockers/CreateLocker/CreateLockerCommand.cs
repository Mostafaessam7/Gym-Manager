using GymManager.Application.Lockers.Contracts;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;

namespace GymManager.Application.Lockers.CreateLocker;

public sealed record CreateLockerCommand(Guid BranchId, string Number) : ICommand<Result<LockerResponse>>;
