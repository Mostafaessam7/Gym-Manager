using GymManager.Application.Abstractions;
using GymManager.Domain.Abstractions;
using GymManager.Domain.Identity;
using GymManager.Domain.Identity.Errors;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;

namespace GymManager.Application.Identity.Users.UpdateUser;

public sealed class UpdateUserCommandHandler(
    IUserRepository userRepository, IUnitOfWork unitOfWork, IBranchAccessGuard branchAccessGuard)
    : ICommandHandler<UpdateUserCommand>
{
    public async Task<Result> Handle(UpdateUserCommand command, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(command.UserId, cancellationToken);
        if (user is null)
            return Result.Failure(UserErrors.NotFound);

        if (user.BranchId.HasValue)
        {
            var accessResult = branchAccessGuard.EnsureCanAccess(user.BranchId.Value);
            if (accessResult.IsFailure)
                return accessResult;
        }

        user.UpdateProfile(command.FirstName, command.LastName, command.PhoneNumber);
        userRepository.Update(user);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
