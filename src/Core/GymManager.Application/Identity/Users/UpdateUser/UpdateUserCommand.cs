using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;

namespace GymManager.Application.Identity.Users.UpdateUser;

public sealed record UpdateUserCommand(Guid UserId, string FirstName, string LastName, string? PhoneNumber) : ICommand;
