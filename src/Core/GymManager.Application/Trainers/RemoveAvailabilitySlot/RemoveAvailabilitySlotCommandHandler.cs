using GymManager.Application.Abstractions;
using GymManager.Application.Trainers.GetTrainers;
using GymManager.Domain.Abstractions;
using GymManager.Domain.Trainers;
using GymManager.Domain.Trainers.Errors;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;

namespace GymManager.Application.Trainers.RemoveAvailabilitySlot;

public sealed class RemoveAvailabilitySlotCommandHandler(
    ITrainerRepository trainerRepository, IUnitOfWork unitOfWork, IBranchAccessGuard branchAccessGuard, ICacheService cacheService)
    : ICommandHandler<RemoveAvailabilitySlotCommand>
{
    public async Task<Result> Handle(RemoveAvailabilitySlotCommand command, CancellationToken cancellationToken)
    {
        var trainer = await trainerRepository.GetByIdAsync(command.TrainerId, cancellationToken);
        if (trainer is null)
            return Result.Failure(TrainerErrors.NotFound);

        var accessResult = branchAccessGuard.EnsureCanAccess(trainer.BranchId);
        if (accessResult.IsFailure)
            return accessResult;

        var result = trainer.RemoveAvailabilitySlot(command.DayOfWeek, command.StartTime, command.EndTime);
        if (result.IsFailure)
            return result;

        trainerRepository.Update(trainer);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        GetTrainersQueryHandler.InvalidateCache(cacheService);

        return Result.Success();
    }
}
