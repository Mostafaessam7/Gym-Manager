using GymManager.Application.Abstractions;
using GymManager.Application.Crm.Contracts;
using GymManager.Domain.Crm.Errors;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace GymManager.Application.Crm.GetLeadById;

public sealed class GetLeadByIdQueryHandler(IApplicationReadDb readDb, IBranchAccessGuard branchAccessGuard) : IQueryHandler<GetLeadByIdQuery, Result<LeadResponse>>
{
    public async Task<Result<LeadResponse>> Handle(GetLeadByIdQuery query, CancellationToken cancellationToken)
    {
        var lead = await readDb.Leads.FirstOrDefaultAsync(l => l.Id == query.LeadId, cancellationToken);
        if (lead is null)
            return Result.Failure<LeadResponse>(LeadErrors.NotFound);

        if (lead.BranchId.HasValue)
        {
            var accessResult = branchAccessGuard.EnsureCanAccess(lead.BranchId.Value);
            if (accessResult.IsFailure)
                return Result.Failure<LeadResponse>(accessResult.Error);
        }

        return Result.Success(lead.ToResponse());
    }
}
