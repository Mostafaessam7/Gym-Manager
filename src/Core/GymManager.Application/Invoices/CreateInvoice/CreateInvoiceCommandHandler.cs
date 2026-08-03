using GymManager.Application.Abstractions;
using GymManager.Application.Invoices.Contracts;
using GymManager.Domain.Abstractions;
using GymManager.Domain.Common;
using GymManager.Domain.Invoices;
using GymManager.Domain.Invoices.Errors;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;

namespace GymManager.Application.Invoices.CreateInvoice;

public sealed class CreateInvoiceCommandHandler(IInvoiceRepository invoiceRepository, IUnitOfWork unitOfWork, IBranchAccessGuard branchAccessGuard)
    : ICommandHandler<CreateInvoiceCommand, Result<InvoiceResponse>>
{
    public async Task<Result<InvoiceResponse>> Handle(CreateInvoiceCommand command, CancellationToken cancellationToken)
    {
        var accessResult = branchAccessGuard.EnsureCanAccess(command.BranchId);
        if (accessResult.IsFailure)
            return Result.Failure<InvoiceResponse>(accessResult.Error);

        if (command.Lines.Count == 0)
            return Result.Failure<InvoiceResponse>(InvoiceErrors.NoLines);

        var nextNumber = await invoiceRepository.GetTotalCountAsync(cancellationToken) + 1;
        var invoiceNumber = $"INV-{nextNumber:D6}";

        var invoice = Invoice.CreateDraft(invoiceNumber, command.MemberId, command.BranchId, command.DueOnUtc);

        foreach (var line in command.Lines)
        {
            var priceResult = Money.Create(line.UnitPrice, command.Currency);
            if (priceResult.IsFailure)
                return Result.Failure<InvoiceResponse>(priceResult.Error);

            invoice.AddLine(line.Description, line.Quantity, priceResult.Value);
        }

        invoiceRepository.Add(invoice);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(invoice.ToResponse());
    }
}
