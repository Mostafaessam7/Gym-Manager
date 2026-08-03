using GymManager.SharedKernel.Cqrs;

namespace GymManager.Application.Branches.UpdateBranch;

public sealed record UpdateBranchCommand(
    Guid BranchId,
    string Name,
    string Country,
    string? Street,
    string? City,
    string? State,
    string? PostalCode,
    string? PhoneNumber,
    string? Email) : ICommand;
