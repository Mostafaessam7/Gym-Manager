using GymManager.Application.Abstractions;
using GymManager.Application.Lockers.Contracts;
using GymManager.Domain.Abstractions;
using GymManager.Domain.Lockers;
using GymManager.Domain.Lockers.Errors;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;

namespace GymManager.Application.Lockers.CreateLocker;

public sealed class CreateLockerCommandHandler(
    ILockerRepository lockerRepository, IUnitOfWork unitOfWork, IBranchAccessGuard branchAccessGuard)
    : ICommandHandler<CreateLockerCommand, Result<LockerResponse>>
{
    public async Task<Result<LockerResponse>> Handle(CreateLockerCommand command, CancellationToken cancellationToken)
    {
        var accessResult = branchAccessGuard.EnsureCanAccess(command.BranchId);
        if (accessResult.IsFailure)
            return Result.Failure<LockerResponse>(accessResult.Error);

        if (await lockerRepository.NumberExistsAsync(command.BranchId, command.Number.Trim(), cancellationToken))
            return Result.Failure<LockerResponse>(LockerErrors.NumberAlreadyInUse(command.Number));

        var locker = Locker.Create(command.BranchId, command.Number);

        lockerRepository.Add(locker);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(locker.ToResponse());
    }
}
