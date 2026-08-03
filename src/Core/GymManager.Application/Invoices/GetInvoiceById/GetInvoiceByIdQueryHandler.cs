using GymManager.Application.Abstractions;
using GymManager.Application.Invoices.Contracts;
using GymManager.Domain.Invoices.Errors;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace GymManager.Application.Invoices.GetInvoiceById;

public sealed class GetInvoiceByIdQueryHandler(IApplicationReadDb readDb, IBranchAccessGuard branchAccessGuard) : IQueryHandler<GetInvoiceByIdQuery, Result<InvoiceResponse>>
{
    public async Task<Result<InvoiceResponse>> Handle(GetInvoiceByIdQuery query, CancellationToken cancellationToken)
    {
        var invoice = await readDb.Invoices.FirstOrDefaultAsync(i => i.Id == query.InvoiceId, cancellationToken);
        if (invoice is null)
            return Result.Failure<InvoiceResponse>(InvoiceErrors.NotFound);

        var accessResult = branchAccessGuard.EnsureCanAccess(invoice.BranchId);
        if (accessResult.IsFailure)
            return Result.Failure<InvoiceResponse>(accessResult.Error);

        return Result.Success(invoice.ToResponse());
    }
}
