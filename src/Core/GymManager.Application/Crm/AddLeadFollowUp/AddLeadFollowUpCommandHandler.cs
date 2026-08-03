using GymManager.Application.Abstractions;
using GymManager.Application.Crm.Contracts;
using GymManager.Domain.Abstractions;
using GymManager.Domain.Crm;
using GymManager.Domain.Crm.Errors;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;

namespace GymManager.Application.Crm.AddLeadFollowUp;

public sealed class AddLeadFollowUpCommandHandler(ILeadRepository leadRepository, IUnitOfWork unitOfWork, IBranchAccessGuard branchAccessGuard)
    : ICommandHandler<AddLeadFollowUpCommand, Result<LeadFollowUpResponse>>
{
    public async Task<Result<LeadFollowUpResponse>> Handle(AddLeadFollowUpCommand command, CancellationToken cancellationToken)
    {
        var lead = await leadRepository.GetByIdAsync(command.LeadId, cancellationToken);
        if (lead is null)
            return Result.Failure<LeadFollowUpResponse>(LeadErrors.NotFound);

        if (lead.BranchId.HasValue)
        {
            var accessResult = branchAccessGuard.EnsureCanAccess(lead.BranchId.Value);
            if (accessResult.IsFailure)
                return Result.Failure<LeadFollowUpResponse>(accessResult.Error);
        }

        var followUp = lead.AddFollowUp(command.Type, command.ScheduledOnUtc, command.Notes);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new LeadFollowUpResponse(
            followUp.Id, followUp.Type.ToString(), followUp.ScheduledOnUtc, followUp.CompletedOnUtc, followUp.Notes, followUp.IsCompleted));
    }
}
