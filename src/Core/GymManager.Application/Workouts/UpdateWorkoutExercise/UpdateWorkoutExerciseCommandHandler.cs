using GymManager.Application.Abstractions;
using GymManager.Domain.Abstractions;
using GymManager.Domain.Workouts;
using GymManager.Domain.Workouts.Errors;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace GymManager.Application.Workouts.UpdateWorkoutExercise;

public sealed class UpdateWorkoutExerciseCommandHandler(
    IWorkoutPlanRepository workoutPlanRepository, IUnitOfWork unitOfWork, IApplicationReadDb readDb, IBranchAccessGuard branchAccessGuard)
    : ICommandHandler<UpdateWorkoutExerciseCommand>
{
    public async Task<Result> Handle(UpdateWorkoutExerciseCommand command, CancellationToken cancellationToken)
    {
        var plan = await workoutPlanRepository.GetByIdAsync(command.PlanId, cancellationToken);
        if (plan is null)
            return Result.Failure(WorkoutErrors.PlanNotFound);

        var member = await readDb.Members.FirstOrDefaultAsync(m => m.Id == plan.MemberId, cancellationToken);
        if (member is not null)
        {
            var accessResult = branchAccessGuard.EnsureCanAccess(member.BranchId);
            if (accessResult.IsFailure)
                return Result.Failure(accessResult.Error);
        }

        var result = plan.UpdateExercise(
            command.ExerciseId, command.Exercise.ExerciseName, command.Exercise.DayNumber, command.Exercise.Order,
            command.Exercise.Sets, command.Exercise.Reps, command.Exercise.WeightKg, command.Exercise.DurationSeconds,
            command.Exercise.RestSeconds, command.Exercise.Notes);
        if (result.IsFailure)
            return result;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
