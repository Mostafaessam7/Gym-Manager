using GymManager.Application.Abstractions;
using GymManager.Application.Nutrition.Contracts;
using GymManager.Domain.Abstractions;
using GymManager.Domain.Members.Errors;
using GymManager.Domain.Nutrition;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace GymManager.Application.Nutrition.RecordNutritionLog;

public sealed class RecordNutritionLogCommandHandler(
    IApplicationReadDb readDb, INutritionLogRepository nutritionLogRepository, IUnitOfWork unitOfWork, IBranchAccessGuard branchAccessGuard)
    : ICommandHandler<RecordNutritionLogCommand, Result<NutritionLogResponse>>
{
    public async Task<Result<NutritionLogResponse>> Handle(RecordNutritionLogCommand command, CancellationToken cancellationToken)
    {
        var member = await readDb.Members.FirstOrDefaultAsync(m => m.Id == command.MemberId, cancellationToken);
        if (member is null)
            return Result.Failure<NutritionLogResponse>(MemberErrors.NotFound);

        var accessResult = branchAccessGuard.EnsureCanAccess(member.BranchId);
        if (accessResult.IsFailure)
            return Result.Failure<NutritionLogResponse>(accessResult.Error);

        var log = NutritionLog.Record(command.MemberId, command.NutritionPlanId, command.LoggedOn, command.Notes);

        foreach (var entry in command.Entries)
            log.AddEntry(entry.FoodName, entry.Calories, entry.ProteinG, entry.CarbsG, entry.FatG, entry.Notes);

        nutritionLogRepository.Add(log);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(log.ToResponse());
    }
}
