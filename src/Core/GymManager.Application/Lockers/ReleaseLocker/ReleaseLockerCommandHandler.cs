using GymManager.Application.Abstractions;
using GymManager.Domain.Abstractions;
using GymManager.Domain.Lockers;
using GymManager.Domain.Lockers.Errors;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;

namespace GymManager.Application.Lockers.ReleaseLocker;

public sealed class ReleaseLockerCommandHandler(
    ILockerRepository lockerRepository, IUnitOfWork unitOfWork, IBranchAccessGuard branchAccessGuard)
    : ICommandHandler<ReleaseLockerCommand>
{
    public async Task<Result> Handle(ReleaseLockerCommand command, CancellationToken cancellationToken)
    {
        var locker = await lockerRepository.GetByIdAsync(command.LockerId, cancellationToken);
        if (locker is null)
            return Result.Failure(LockerErrors.NotFound);

        var accessResult = branchAccessGuard.EnsureCanAccess(locker.BranchId);
        if (accessResult.IsFailure)
            return accessResult;

        var result = locker.Release();
        if (result.IsFailure)
            return result;

        lockerRepository.Update(locker);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
