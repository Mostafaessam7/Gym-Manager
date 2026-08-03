using GymManager.Application.Abstractions;
using GymManager.Domain.Abstractions;
using GymManager.Domain.Classes;
using GymManager.Domain.Classes.Errors;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;

namespace GymManager.Application.Classes.UpdateGymClass;

public sealed class UpdateGymClassCommandHandler(
    IGymClassRepository gymClassRepository, IUnitOfWork unitOfWork, IBranchAccessGuard branchAccessGuard)
    : ICommandHandler<UpdateGymClassCommand>
{
    public async Task<Result> Handle(UpdateGymClassCommand command, CancellationToken cancellationToken)
    {
        var gymClass = await gymClassRepository.GetByIdAsync(command.GymClassId, cancellationToken);
        if (gymClass is null)
            return Result.Failure(GymClassErrors.NotFound);

        var accessResult = branchAccessGuard.EnsureCanAccess(gymClass.BranchId);
        if (accessResult.IsFailure)
            return accessResult;

        gymClass.Update(command.Name, command.Description, command.TrainerId, command.Capacity, command.DurationMinutes);

        gymClassRepository.Update(gymClass);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
