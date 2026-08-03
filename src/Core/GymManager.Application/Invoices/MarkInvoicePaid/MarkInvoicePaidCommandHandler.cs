using GymManager.Application.Abstractions;
using GymManager.Domain.Abstractions;
using GymManager.Domain.Invoices;
using GymManager.Domain.Invoices.Errors;
using GymManager.Domain.Payments;
using GymManager.Domain.Payments.Errors;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;

namespace GymManager.Application.Invoices.MarkInvoicePaid;

public sealed class MarkInvoicePaidCommandHandler(
    IInvoiceRepository invoiceRepository, IPaymentRepository paymentRepository, IUnitOfWork unitOfWork,
    IBranchAccessGuard branchAccessGuard)
    : ICommandHandler<MarkInvoicePaidCommand>
{
    public async Task<Result> Handle(MarkInvoicePaidCommand command, CancellationToken cancellationToken)
    {
        var invoice = await invoiceRepository.GetByIdAsync(command.InvoiceId, cancellationToken);
        if (invoice is null)
            return Result.Failure(InvoiceErrors.NotFound);

        var accessResult = branchAccessGuard.EnsureCanAccess(invoice.BranchId);
        if (accessResult.IsFailure)
            return accessResult;

        var payment = await paymentRepository.GetByIdAsync(command.PaymentId, cancellationToken);
        if (payment is null)
            return Result.Failure(PaymentErrors.NotFound);

        var result = invoice.MarkPaid(payment.Id);
        if (result.IsFailure)
            return result;

        invoiceRepository.Update(invoice);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
