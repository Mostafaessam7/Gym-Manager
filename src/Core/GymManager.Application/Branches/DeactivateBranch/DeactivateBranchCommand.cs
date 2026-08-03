using GymManager.SharedKernel.Cqrs;

namespace GymManager.Application.Branches.DeactivateBranch;

public sealed record DeactivateBranchCommand(Guid BranchId) : ICommand;
