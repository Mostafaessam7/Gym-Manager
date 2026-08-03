using GymManager.Application.Abstractions;
using GymManager.Application.Identity.Contracts;
using GymManager.Domain.Abstractions;
using GymManager.Domain.Common;
using GymManager.Domain.Identity;
using GymManager.Domain.Identity.Errors;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;

namespace GymManager.Application.Identity.Users.CreateUser;

public sealed class CreateUserCommandHandler(
    IUserRepository userRepository,
    IRoleRepository roleRepository,
    IPasswordHasher passwordHasher,
    IUnitOfWork unitOfWork,
    IBranchAccessGuard branchAccessGuard)
    : ICommandHandler<CreateUserCommand, Result<UserResponse>>
{
    public async Task<Result<UserResponse>> Handle(CreateUserCommand command, CancellationToken cancellationToken)
    {
        if (command.BranchId.HasValue)
        {
            var accessResult = branchAccessGuard.EnsureCanAccess(command.BranchId.Value);
            if (accessResult.IsFailure)
                return Result.Failure<UserResponse>(accessResult.Error);
        }

        var emailResult = Email.Create(command.Email);
        if (emailResult.IsFailure)
            return Result.Failure<UserResponse>(emailResult.Error);

        if (await userRepository.EmailExistsAsync(emailResult.Value.Value, cancellationToken))
            return Result.Failure<UserResponse>(UserErrors.EmailAlreadyInUse(emailResult.Value.Value));

        var roles = await roleRepository.GetByIdsAsync(command.RoleIds, cancellationToken);

        var user = User.Register(
            emailResult.Value,
            passwordHasher.Hash(command.Password),
            command.FirstName,
            command.LastName,
            command.BranchId);

        foreach (var role in roles)
            user.AssignRole(role.Id);

        userRepository.Add(user);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new UserResponse(
            user.Id,
            user.Email.Value,
            user.FirstName,
            user.LastName,
            user.PhoneNumber,
            user.IsActive,
            user.BranchId,
            roles.Select(r => r.Name).ToArray(),
            user.CreatedOnUtc));
    }
}
