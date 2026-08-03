using GymManager.Application.Abstractions;
using GymManager.Domain.Abstractions;
using GymManager.Domain.Invoices;
using GymManager.Domain.Invoices.Errors;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;

namespace GymManager.Application.Invoices.VoidInvoice;

public sealed class VoidInvoiceCommandHandler(
    IInvoiceRepository invoiceRepository, IUnitOfWork unitOfWork, IBranchAccessGuard branchAccessGuard)
    : ICommandHandler<VoidInvoiceCommand>
{
    public async Task<Result> Handle(VoidInvoiceCommand command, CancellationToken cancellationToken)
    {
        var invoice = await invoiceRepository.GetByIdAsync(command.InvoiceId, cancellationToken);
        if (invoice is null)
            return Result.Failure(InvoiceErrors.NotFound);

        var accessResult = branchAccessGuard.EnsureCanAccess(invoice.BranchId);
        if (accessResult.IsFailure)
            return accessResult;

        var result = invoice.Void();
        if (result.IsFailure)
            return result;

        invoiceRepository.Update(invoice);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
