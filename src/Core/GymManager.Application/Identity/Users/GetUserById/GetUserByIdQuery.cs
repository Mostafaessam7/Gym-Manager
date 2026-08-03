using GymManager.Application.Identity.Contracts;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;

namespace GymManager.Application.Identity.Users.GetUserById;

public sealed record GetUserByIdQuery(Guid UserId) : IQuery<Result<UserResponse>>;
