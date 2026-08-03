using GymManager.Application.Abstractions;
using GymManager.Application.Trainers.GetTrainers;
using GymManager.Domain.Abstractions;
using GymManager.Domain.Common;
using GymManager.Domain.Trainers;
using GymManager.Domain.Trainers.Errors;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;

namespace GymManager.Application.Trainers.UpdateTrainer;

public sealed class UpdateTrainerCommandHandler(
    ITrainerRepository trainerRepository, IUnitOfWork unitOfWork, IBranchAccessGuard branchAccessGuard, ICacheService cacheService)
    : ICommandHandler<UpdateTrainerCommand>
{
    public async Task<Result> Handle(UpdateTrainerCommand command, CancellationToken cancellationToken)
    {
        var trainer = await trainerRepository.GetByIdAsync(command.TrainerId, cancellationToken);
        if (trainer is null)
            return Result.Failure(TrainerErrors.NotFound);

        var accessResult = branchAccessGuard.EnsureCanAccess(trainer.BranchId);
        if (accessResult.IsFailure)
            return accessResult;

        Email? email = null;
        if (!string.IsNullOrWhiteSpace(command.Email))
        {
            var emailResult = Email.Create(command.Email);
            if (emailResult.IsFailure)
                return emailResult;

            email = emailResult.Value;
        }

        trainer.UpdateProfile(command.FirstName, command.LastName, command.Specialization, command.Bio, command.PhoneNumber, email);

        trainerRepository.Update(trainer);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        GetTrainersQueryHandler.InvalidateCache(cacheService);

        return Result.Success();
    }
}
