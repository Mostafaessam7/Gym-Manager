using GymManager.Application.Identity.Contracts;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;

namespace GymManager.Application.Identity.Users.CreateUser;

public sealed record CreateUserCommand(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    Guid? BranchId,
    IReadOnlyCollection<Guid> RoleIds) : ICommand<Result<UserResponse>>;
