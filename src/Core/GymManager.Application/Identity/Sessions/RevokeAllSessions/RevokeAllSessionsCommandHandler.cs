using GymManager.Application.Abstractions;
using GymManager.Domain.Abstractions;
using GymManager.Domain.Identity;
using GymManager.Domain.Identity.Errors;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;

namespace GymManager.Application.Identity.Sessions.RevokeAllSessions;

public sealed class RevokeAllSessionsCommandHandler(IUserRepository userRepository, IUnitOfWork unitOfWork)
    : ICommandHandler<RevokeAllSessionsCommand>
{
    public async Task<Result> Handle(RevokeAllSessionsCommand command, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(command.UserId, cancellationToken);
        if (user is null)
            return Result.Failure(UserErrors.NotFound);

        user.RevokeAllSessions();

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
