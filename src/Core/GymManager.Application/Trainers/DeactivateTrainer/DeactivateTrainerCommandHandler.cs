using GymManager.Application.Abstractions;
using GymManager.Application.Trainers.GetTrainers;
using GymManager.Domain.Abstractions;
using GymManager.Domain.Trainers;
using GymManager.Domain.Trainers.Errors;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;

namespace GymManager.Application.Trainers.DeactivateTrainer;

public sealed class DeactivateTrainerCommandHandler(
    ITrainerRepository trainerRepository, IUnitOfWork unitOfWork, IBranchAccessGuard branchAccessGuard, ICacheService cacheService)
    : ICommandHandler<DeactivateTrainerCommand>
{
    public async Task<Result> Handle(DeactivateTrainerCommand command, CancellationToken cancellationToken)
    {
        var trainer = await trainerRepository.GetByIdAsync(command.TrainerId, cancellationToken);
        if (trainer is null)
            return Result.Failure(TrainerErrors.NotFound);

        var accessResult = branchAccessGuard.EnsureCanAccess(trainer.BranchId);
        if (accessResult.IsFailure)
            return accessResult;

        trainer.Deactivate();

        trainerRepository.Update(trainer);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        GetTrainersQueryHandler.InvalidateCache(cacheService);

        return Result.Success();
    }
}
