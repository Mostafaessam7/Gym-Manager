using GymManager.Application.Abstractions;
using GymManager.Application.Trainers.Contracts;
using GymManager.Application.Trainers.GetTrainers;
using GymManager.Domain.Abstractions;
using GymManager.Domain.Common;
using GymManager.Domain.Trainers;
using GymManager.Domain.Trainers.Errors;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;

namespace GymManager.Application.Trainers.CreateTrainer;

public sealed class CreateTrainerCommandHandler(
    ITrainerRepository trainerRepository, IUnitOfWork unitOfWork, IBranchAccessGuard branchAccessGuard, ICacheService cacheService)
    : ICommandHandler<CreateTrainerCommand, Result<TrainerResponse>>
{
    public async Task<Result<TrainerResponse>> Handle(CreateTrainerCommand command, CancellationToken cancellationToken)
    {
        var accessResult = branchAccessGuard.EnsureCanAccess(command.BranchId);
        if (accessResult.IsFailure)
            return Result.Failure<TrainerResponse>(accessResult.Error);

        Email? email = null;
        if (!string.IsNullOrWhiteSpace(command.Email))
        {
            var emailResult = Email.Create(command.Email);
            if (emailResult.IsFailure)
                return Result.Failure<TrainerResponse>(emailResult.Error);

            if (await trainerRepository.EmailExistsAsync(emailResult.Value.Value, cancellationToken))
                return Result.Failure<TrainerResponse>(TrainerErrors.EmailAlreadyInUse(emailResult.Value.Value));

            email = emailResult.Value;
        }

        var trainer = Trainer.Create(
            command.BranchId, command.FirstName, command.LastName, command.Specialization, command.Bio, command.PhoneNumber, email, command.UserId);

        trainerRepository.Add(trainer);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        GetTrainersQueryHandler.InvalidateCache(cacheService);

        return Result.Success(trainer.ToResponse());
    }
}
