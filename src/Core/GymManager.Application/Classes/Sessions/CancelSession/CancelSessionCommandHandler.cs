using GymManager.Application.Abstractions;
using GymManager.Domain.Abstractions;
using GymManager.Domain.Classes;
using GymManager.Domain.Classes.Errors;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;

namespace GymManager.Application.Classes.Sessions.CancelSession;

public sealed class CancelSessionCommandHandler(
    IClassSessionRepository sessionRepository, IUnitOfWork unitOfWork, IBranchAccessGuard branchAccessGuard)
    : ICommandHandler<CancelSessionCommand>
{
    public async Task<Result> Handle(CancelSessionCommand command, CancellationToken cancellationToken)
    {
        var session = await sessionRepository.GetByIdAsync(command.SessionId, cancellationToken);
        if (session is null)
            return Result.Failure(ClassSessionErrors.NotFound);

        var accessResult = branchAccessGuard.EnsureCanAccess(session.BranchId);
        if (accessResult.IsFailure)
            return accessResult;

        var result = session.Cancel();
        if (result.IsFailure)
            return result;

        sessionRepository.Update(session);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
