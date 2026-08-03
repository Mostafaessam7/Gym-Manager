using GymManager.Application.Abstractions;
using GymManager.Application.Crm.Contracts;
using GymManager.Domain.Abstractions;
using GymManager.Domain.Crm;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;

namespace GymManager.Application.Crm.CreateLead;

public sealed class CreateLeadCommandHandler(ILeadRepository leadRepository, IUnitOfWork unitOfWork, IBranchAccessGuard branchAccessGuard)
    : ICommandHandler<CreateLeadCommand, Result<LeadResponse>>
{
    public async Task<Result<LeadResponse>> Handle(CreateLeadCommand command, CancellationToken cancellationToken)
    {
        if (command.BranchId.HasValue)
        {
            var accessResult = branchAccessGuard.EnsureCanAccess(command.BranchId.Value);
            if (accessResult.IsFailure)
                return Result.Failure<LeadResponse>(accessResult.Error);
        }

        var lead = Lead.Create(command.Name, command.Email, command.Phone, command.Source, command.BranchId, command.AssignedToUserId, command.Notes);

        leadRepository.Add(lead);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(lead.ToResponse());
    }
}
