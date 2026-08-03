using GymManager.Application.Abstractions;
using GymManager.Domain.Abstractions;
using GymManager.Domain.Classes;
using GymManager.Domain.Classes.Errors;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;

namespace GymManager.Application.Classes.Sessions.CancelBooking;

public sealed class CancelBookingCommandHandler(
    IClassSessionRepository sessionRepository, IUnitOfWork unitOfWork, IBranchAccessGuard branchAccessGuard)
    : ICommandHandler<CancelBookingCommand>
{
    public async Task<Result> Handle(CancelBookingCommand command, CancellationToken cancellationToken)
    {
        var session = await sessionRepository.GetByIdAsync(command.SessionId, cancellationToken);
        if (session is null)
            return Result.Failure(ClassSessionErrors.NotFound);

        var accessResult = branchAccessGuard.EnsureCanAccess(session.BranchId);
        if (accessResult.IsFailure)
            return accessResult;

        var result = session.CancelBooking(command.MemberId);
        if (result.IsFailure)
            return result;

        sessionRepository.Update(session);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
