using GymManager.Domain.Common;
using GymManager.Domain.Invoices.Errors;
using GymManager.Domain.Invoices.Events;
using GymManager.SharedKernel.Auditing;
using GymManager.SharedKernel.Primitives;
using GymManager.SharedKernel.Results;

namespace GymManager.Domain.Invoices;

/// <summary>A billing document issued to a member, made up of one or more <see cref="InvoiceLine"/>s.</summary>
public sealed class Invoice : AggregateRoot<Guid>, IAuditableEntity
{
    private readonly List<InvoiceLine> _lines = [];

    private Invoice()
    {
        InvoiceNumber = string.Empty;
    }

    private Invoice(Guid id, string invoiceNumber, Guid memberId, Guid branchId, DateTimeOffset issuedOnUtc, DateTimeOffset dueOnUtc)
        : base(id)
    {
        InvoiceNumber = invoiceNumber;
        MemberId = memberId;
        BranchId = branchId;
        IssuedOnUtc = issuedOnUtc;
        DueOnUtc = dueOnUtc;
        Status = InvoiceStatus.Draft;
    }

    public string InvoiceNumber { get; private set; }

    public Guid MemberId { get; private set; }

    public Guid BranchId { get; private set; }

    public DateTimeOffset IssuedOnUtc { get; private set; }

    public DateTimeOffset DueOnUtc { get; private set; }

    public InvoiceStatus Status { get; private set; }

    public Guid? PaymentId { get; private set; }

    public IReadOnlyCollection<InvoiceLine> Lines => _lines.AsReadOnly();

    public string Currency => _lines.Count > 0 ? _lines[0].UnitPrice.Currency : Money.DefaultCurrency;

    public Money TotalAmount => _lines.Aggregate(Money.Zero(Currency), (total, line) => total.Add(line.LineTotal));

    public DateTimeOffset CreatedOnUtc { get; private set; }

    public string? CreatedBy { get; private set; }

    public DateTimeOffset? ModifiedOnUtc { get; private set; }

    public string? ModifiedBy { get; private set; }

    public static Invoice CreateDraft(string invoiceNumber, Guid memberId, Guid branchId, DateTimeOffset dueOnUtc) =>
        new(Guid.NewGuid(), invoiceNumber, memberId, branchId, DateTimeOffset.UtcNow, dueOnUtc);

    public Result AddLine(string description, int quantity, Money unitPrice)
    {
        if (Status != InvoiceStatus.Draft)
            return Result.Failure(InvoiceErrors.NotDraft);

        _lines.Add(new InvoiceLine(description, quantity, unitPrice));
        return Result.Success();
    }

    public Result Issue()
    {
        if (Status != InvoiceStatus.Draft)
            return Result.Failure(InvoiceErrors.NotDraft);

        if (_lines.Count == 0)
            return Result.Failure(InvoiceErrors.NoLines);

        Status = InvoiceStatus.Issued;
        Raise(new InvoiceIssuedDomainEvent(Id, MemberId));

        return Result.Success();
    }

    public Result MarkPaid(Guid paymentId)
    {
        if (Status != InvoiceStatus.Issued)
            return Result.Failure(InvoiceErrors.NotIssued);

        Status = InvoiceStatus.Paid;
        PaymentId = paymentId;
        Raise(new InvoicePaidDomainEvent(Id, MemberId));

        return Result.Success();
    }

    public Result Void()
    {
        if (Status == InvoiceStatus.Paid)
            return Result.Failure(InvoiceErrors.AlreadyPaid);

        Status = InvoiceStatus.Void;
        Raise(new InvoiceVoidedDomainEvent(Id));

        return Result.Success();
    }

    public void SetCreated(DateTimeOffset onUtc, string? by)
    {
        CreatedOnUtc = onUtc;
        CreatedBy = by;
    }

    public void SetModified(DateTimeOffset onUtc, string? by)
    {
        ModifiedOnUtc = onUtc;
        ModifiedBy = by;
    }
}
