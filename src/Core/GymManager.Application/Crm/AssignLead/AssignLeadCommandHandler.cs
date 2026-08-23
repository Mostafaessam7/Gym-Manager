using GymManager.Application.Abstractions;
using GymManager.Domain.Abstractions;
using GymManager.Domain.Crm;
using GymManager.Domain.Crm.Errors;
using GymManager.Domain.Identity.Errors;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace GymManager.Application.Crm.AssignLead;

public sealed class AssignLeadCommandHandler(
    ILeadRepository leadRepository, IApplicationReadDb readDb, IUnitOfWork unitOfWork, IBranchAccessGuard branchAccessGuard)
    : ICommandHandler<AssignLeadCommand>
{
    public async Task<Result> Handle(AssignLeadCommand command, CancellationToken cancellationToken)
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

        // Pre-checked (rather than letting the Leads.AssignedToUserId foreign key reject it) so a bad id
        // comes back as a normal Result.Failure instead of an unhandled DbUpdateException — this codebase
        // has no global exception handler to translate that into a ProblemDetails response.
        var userExists = await readDb.Users.AnyAsync(u => u.Id == command.UserId, cancellationToken);
        if (!userExists)
            return Result.Failure(UserErrors.NotFound);

        lead.AssignTo(command.UserId);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
