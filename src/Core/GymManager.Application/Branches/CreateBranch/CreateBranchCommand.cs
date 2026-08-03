using GymManager.Application.Branches.Contracts;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;

namespace GymManager.Application.Branches.CreateBranch;

public sealed record CreateBranchCommand(
    string Name,
    string Country,
    string? Street,
    string? City,
    string? State,
    string? PostalCode,
    string? PhoneNumber,
    string? Email) : ICommand<Result<BranchResponse>>;
