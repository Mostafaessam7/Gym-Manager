using GymManager.Application.Abstractions;
using GymManager.Application.Workouts.Contracts;
using GymManager.Domain.Abstractions;
using GymManager.Domain.Members.Errors;
using GymManager.Domain.Workouts;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace GymManager.Application.Workouts.RecordWorkoutLog;

public sealed class RecordWorkoutLogCommandHandler(
    IApplicationReadDb readDb, IWorkoutLogRepository workoutLogRepository, IUnitOfWork unitOfWork, IBranchAccessGuard branchAccessGuard)
    : ICommandHandler<RecordWorkoutLogCommand, Result<WorkoutLogResponse>>
{
    public async Task<Result<WorkoutLogResponse>> Handle(RecordWorkoutLogCommand command, CancellationToken cancellationToken)
    {
        var member = await readDb.Members.FirstOrDefaultAsync(m => m.Id == command.MemberId, cancellationToken);
        if (member is null)
            return Result.Failure<WorkoutLogResponse>(MemberErrors.NotFound);

        var accessResult = branchAccessGuard.EnsureCanAccess(member.BranchId);
        if (accessResult.IsFailure)
            return Result.Failure<WorkoutLogResponse>(accessResult.Error);

        var log = WorkoutLog.Record(command.MemberId, command.WorkoutPlanId, command.CompletedOnUtc, command.DurationMinutes, command.Notes);

        foreach (var exercise in command.Exercises)
            log.AddExercise(exercise.ExerciseName, exercise.SetsCompleted, exercise.RepsCompleted, exercise.WeightKg, exercise.Notes);

        workoutLogRepository.Add(log);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(log.ToResponse());
    }
}
