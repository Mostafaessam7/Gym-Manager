using GymManager.Application.Abstractions;
using GymManager.Application.Classes.Contracts;
using GymManager.Domain.Abstractions;
using GymManager.Domain.Classes;
using GymManager.Domain.Classes.Errors;
using GymManager.Domain.Trainers;
using GymManager.Domain.Trainers.Errors;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;

namespace GymManager.Application.Classes.CreateGymClass;

public sealed class CreateGymClassCommandHandler(
    IGymClassRepository gymClassRepository, ITrainerRepository trainerRepository, IUnitOfWork unitOfWork,
    IBranchAccessGuard branchAccessGuard)
    : ICommandHandler<CreateGymClassCommand, Result<GymClassResponse>>
{
    public async Task<Result<GymClassResponse>> Handle(CreateGymClassCommand command, CancellationToken cancellationToken)
    {
        var accessResult = branchAccessGuard.EnsureCanAccess(command.BranchId);
        if (accessResult.IsFailure)
            return Result.Failure<GymClassResponse>(accessResult.Error);

        if (await trainerRepository.GetByIdAsync(command.TrainerId, cancellationToken) is null)
            return Result.Failure<GymClassResponse>(TrainerErrors.NotFound);

        if (await gymClassRepository.NameExistsAsync(command.Name.Trim(), cancellationToken))
            return Result.Failure<GymClassResponse>(GymClassErrors.NameAlreadyInUse(command.Name));

        var gymClass = GymClass.Create(command.Name, command.Description, command.BranchId, command.TrainerId, command.Capacity, command.DurationMinutes);

        gymClassRepository.Add(gymClass);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(gymClass.ToResponse());
    }
}
