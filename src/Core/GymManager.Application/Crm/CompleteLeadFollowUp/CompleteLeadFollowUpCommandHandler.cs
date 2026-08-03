using GymManager.Application.Abstractions;
using GymManager.Domain.Abstractions;
using GymManager.Domain.Crm;
using GymManager.Domain.Crm.Errors;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;

namespace GymManager.Application.Crm.CompleteLeadFollowUp;

public sealed class CompleteLeadFollowUpCommandHandler(ILeadRepository leadRepository, IUnitOfWork unitOfWork, IBranchAccessGuard branchAccessGuard)
    : ICommandHandler<CompleteLeadFollowUpCommand>
{
    public async Task<Result> Handle(CompleteLeadFollowUpCommand command, CancellationToken cancellationToken)
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

        var result = lead.CompleteFollowUp(command.FollowUpId, command.CompletedOnUtc, command.Notes);
        if (result.IsFailure)
            return result;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
