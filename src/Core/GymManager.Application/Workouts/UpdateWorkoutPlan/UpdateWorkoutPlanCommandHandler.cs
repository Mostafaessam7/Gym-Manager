using GymManager.Application.Abstractions;
using GymManager.Domain.Abstractions;
using GymManager.Domain.Workouts;
using GymManager.Domain.Workouts.Errors;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace GymManager.Application.Workouts.UpdateWorkoutPlan;

public sealed class UpdateWorkoutPlanCommandHandler(
    IWorkoutPlanRepository workoutPlanRepository, IUnitOfWork unitOfWork, IApplicationReadDb readDb, IBranchAccessGuard branchAccessGuard)
    : ICommandHandler<UpdateWorkoutPlanCommand>
{
    public async Task<Result> Handle(UpdateWorkoutPlanCommand command, CancellationToken cancellationToken)
    {
        var plan = await workoutPlanRepository.GetByIdAsync(command.PlanId, cancellationToken);
        if (plan is null)
            return Result.Failure(WorkoutErrors.PlanNotFound);

        // IgnoreQueryFilters(): see GetMembershipsByMemberQueryHandler for why this authorization-only lookup
        // must bypass the global branch-isolation filter.
        var member = await readDb.Members.IgnoreQueryFilters().FirstOrDefaultAsync(m => m.Id == plan.MemberId, cancellationToken);
        if (member is not null)
        {
            var accessResult = branchAccessGuard.EnsureCanAccess(member.BranchId);
            if (accessResult.IsFailure)
                return Result.Failure(accessResult.Error);
        }

        plan.UpdateDetails(command.Name, command.Description);

        if (command.IsActive)
            plan.Activate();
        else
            plan.Deactivate();

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
