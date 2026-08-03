using GymManager.Application.Abstractions;
using GymManager.Domain.Abstractions;
using GymManager.Domain.Lockers;
using GymManager.Domain.Lockers.Errors;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;

namespace GymManager.Application.Lockers.AssignLocker;

public sealed class AssignLockerCommandHandler(
    ILockerRepository lockerRepository, IUnitOfWork unitOfWork, IBranchAccessGuard branchAccessGuard)
    : ICommandHandler<AssignLockerCommand>
{
    public async Task<Result> Handle(AssignLockerCommand command, CancellationToken cancellationToken)
    {
        var locker = await lockerRepository.GetByIdAsync(command.LockerId, cancellationToken);
        if (locker is null)
            return Result.Failure(LockerErrors.NotFound);

        var accessResult = branchAccessGuard.EnsureCanAccess(locker.BranchId);
        if (accessResult.IsFailure)
            return accessResult;

        var result = locker.AssignTo(command.MemberId);
        if (result.IsFailure)
            return result;

        lockerRepository.Update(locker);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
