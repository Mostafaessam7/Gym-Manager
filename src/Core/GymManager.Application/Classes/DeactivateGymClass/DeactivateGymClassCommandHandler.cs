using GymManager.Application.Abstractions;
using GymManager.Domain.Abstractions;
using GymManager.Domain.Classes;
using GymManager.Domain.Classes.Errors;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;

namespace GymManager.Application.Classes.DeactivateGymClass;

public sealed class DeactivateGymClassCommandHandler(
    IGymClassRepository gymClassRepository, IUnitOfWork unitOfWork, IBranchAccessGuard branchAccessGuard)
    : ICommandHandler<DeactivateGymClassCommand>
{
    public async Task<Result> Handle(DeactivateGymClassCommand command, CancellationToken cancellationToken)
    {
        var gymClass = await gymClassRepository.GetByIdAsync(command.GymClassId, cancellationToken);
        if (gymClass is null)
            return Result.Failure(GymClassErrors.NotFound);

        var accessResult = branchAccessGuard.EnsureCanAccess(gymClass.BranchId);
        if (accessResult.IsFailure)
            return accessResult;

        gymClass.Deactivate();

        gymClassRepository.Update(gymClass);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
