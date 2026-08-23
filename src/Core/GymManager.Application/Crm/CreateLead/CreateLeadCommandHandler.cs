using GymManager.Application.Abstractions;
using GymManager.Application.Crm.Contracts;
using GymManager.Domain.Abstractions;
using GymManager.Domain.Branches.Errors;
using GymManager.Domain.Crm;
using GymManager.Domain.Identity.Errors;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace GymManager.Application.Crm.CreateLead;

public sealed class CreateLeadCommandHandler(
    ILeadRepository leadRepository, IApplicationReadDb readDb, IUnitOfWork unitOfWork, IBranchAccessGuard branchAccessGuard)
    : ICommandHandler<CreateLeadCommand, Result<LeadResponse>>
{
    public async Task<Result<LeadResponse>> Handle(CreateLeadCommand command, CancellationToken cancellationToken)
    {
        if (command.BranchId.HasValue)
        {
            var accessResult = branchAccessGuard.EnsureCanAccess(command.BranchId.Value);
            if (accessResult.IsFailure)
                return Result.Failure<LeadResponse>(accessResult.Error);

            // Pre-checked (rather than letting the Leads.BranchId foreign key reject it) so a bad id comes
            // back as a normal Result.Failure instead of an unhandled DbUpdateException — this codebase has
            // no global exception handler to translate that into a ProblemDetails response.
            var branchExists = await readDb.Branches.AnyAsync(b => b.Id == command.BranchId.Value, cancellationToken);
            if (!branchExists)
                return Result.Failure<LeadResponse>(BranchErrors.NotFound);
        }

        if (command.AssignedToUserId.HasValue)
        {
            var userExists = await readDb.Users.AnyAsync(u => u.Id == command.AssignedToUserId.Value, cancellationToken);
            if (!userExists)
                return Result.Failure<LeadResponse>(UserErrors.NotFound);
        }

        var lead = Lead.Create(command.Name, command.Email, command.Phone, command.Source, command.BranchId, command.AssignedToUserId, command.Notes);

        leadRepository.Add(lead);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(lead.ToResponse());
    }
}
