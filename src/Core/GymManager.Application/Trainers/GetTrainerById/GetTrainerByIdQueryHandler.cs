using GymManager.Application.Abstractions;
using GymManager.Application.Trainers.Contracts;
using GymManager.Domain.Trainers.Errors;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace GymManager.Application.Trainers.GetTrainerById;

public sealed class GetTrainerByIdQueryHandler(IApplicationReadDb readDb, IBranchAccessGuard branchAccessGuard) : IQueryHandler<GetTrainerByIdQuery, Result<TrainerResponse>>
{
    public async Task<Result<TrainerResponse>> Handle(GetTrainerByIdQuery query, CancellationToken cancellationToken)
    {
        var trainer = await readDb.Trainers.FirstOrDefaultAsync(t => t.Id == query.TrainerId, cancellationToken);
        if (trainer is null)
            return Result.Failure<TrainerResponse>(TrainerErrors.NotFound);

        var accessResult = branchAccessGuard.EnsureCanAccess(trainer.BranchId);
        if (accessResult.IsFailure)
            return Result.Failure<TrainerResponse>(accessResult.Error);

        return Result.Success(trainer.ToResponse());
    }
}
