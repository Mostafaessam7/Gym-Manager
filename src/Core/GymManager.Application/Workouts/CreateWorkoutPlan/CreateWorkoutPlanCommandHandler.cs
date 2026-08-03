using GymManager.Application.Abstractions;
using GymManager.Application.Workouts.Contracts;
using GymManager.Domain.Abstractions;
using GymManager.Domain.Members.Errors;
using GymManager.Domain.Workouts;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace GymManager.Application.Workouts.CreateWorkoutPlan;

public sealed class CreateWorkoutPlanCommandHandler(
    IApplicationReadDb readDb, IWorkoutPlanRepository workoutPlanRepository, IUnitOfWork unitOfWork, IBranchAccessGuard branchAccessGuard)
    : ICommandHandler<CreateWorkoutPlanCommand, Result<WorkoutPlanResponse>>
{
    public async Task<Result<WorkoutPlanResponse>> Handle(CreateWorkoutPlanCommand command, CancellationToken cancellationToken)
    {
        var member = await readDb.Members.FirstOrDefaultAsync(m => m.Id == command.MemberId, cancellationToken);
        if (member is null)
            return Result.Failure<WorkoutPlanResponse>(MemberErrors.NotFound);

        var accessResult = branchAccessGuard.EnsureCanAccess(member.BranchId);
        if (accessResult.IsFailure)
            return Result.Failure<WorkoutPlanResponse>(accessResult.Error);

        var plan = WorkoutPlan.Create(command.MemberId, command.TrainerId, command.Name, command.Description);

        foreach (var exercise in command.Exercises)
        {
            plan.AddExercise(
                exercise.ExerciseName, exercise.DayNumber, exercise.Order, exercise.Sets, exercise.Reps,
                exercise.WeightKg, exercise.DurationSeconds, exercise.RestSeconds, exercise.Notes);
        }

        workoutPlanRepository.Add(plan);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(plan.ToResponse());
    }
}
