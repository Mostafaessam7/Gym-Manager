using GymManager.Application.Abstractions;
using GymManager.Domain.Abstractions;
using GymManager.Domain.Crm;
using GymManager.Domain.Crm.Errors;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;

namespace GymManager.Application.Crm.MoveLeadStage;

public sealed class MoveLeadStageCommandHandler(ILeadRepository leadRepository, IUnitOfWork unitOfWork, IBranchAccessGuard branchAccessGuard)
    : ICommandHandler<MoveLeadStageCommand>
{
    public async Task<Result> Handle(MoveLeadStageCommand command, CancellationToken cancellationToken)
    {
        var lead = await leadRepository.GetByIdAsync(command.LeadId, cancellationToken);
        if (lead is null)
            return Result.Failure(LeadErrors.NotFound);

        if (lead.BranchId.HasValue)
        {
            var accessResult = branchAccessGuard.EnsureCanAccess(lead.BranchId.Value);
            if (accessResult.IsFailure)
                return accessResult;
        }

        var result = lead.MoveToStage(command.Stage);
        if (result.IsFailure)
            return result;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
