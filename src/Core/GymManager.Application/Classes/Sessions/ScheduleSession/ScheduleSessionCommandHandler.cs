using GymManager.Application.Abstractions;
using GymManager.Application.Classes.Contracts;
using GymManager.Domain.Abstractions;
using GymManager.Domain.Classes;
using GymManager.Domain.Classes.Errors;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;

namespace GymManager.Application.Classes.Sessions.ScheduleSession;

public sealed class ScheduleSessionCommandHandler(
    IGymClassRepository gymClassRepository, IClassSessionRepository sessionRepository, IUnitOfWork unitOfWork,
    IBranchAccessGuard branchAccessGuard)
    : ICommandHandler<ScheduleSessionCommand, Result<ClassSessionResponse>>
{
    public async Task<Result<ClassSessionResponse>> Handle(ScheduleSessionCommand command, CancellationToken cancellationToken)
    {
        var gymClass = await gymClassRepository.GetByIdAsync(command.GymClassId, cancellationToken);
        if (gymClass is null)
            return Result.Failure<ClassSessionResponse>(GymClassErrors.NotFound);

        var accessResult = branchAccessGuard.EnsureCanAccess(gymClass.BranchId);
        if (accessResult.IsFailure)
            return Result.Failure<ClassSessionResponse>(accessResult.Error);

        if (await sessionRepository.TrainerHasOverlappingSessionAsync(gymClass.TrainerId, command.StartUtc, command.EndUtc, cancellationToken: cancellationToken))
            return Result.Failure<ClassSessionResponse>(ClassSessionErrors.TrainerOverlap);

        var capacity = command.CapacityOverride ?? gymClass.Capacity;

        var sessionResult = ClassSession.Schedule(gymClass.Id, gymClass.TrainerId, gymClass.BranchId, command.StartUtc, command.EndUtc, capacity);
        if (sessionResult.IsFailure)
            return Result.Failure<ClassSessionResponse>(sessionResult.Error);

        sessionRepository.Add(sessionResult.Value);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(sessionResult.Value.ToResponse());
    }
}
