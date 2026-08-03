using GymManager.Domain.Common;
using GymManager.Domain.Payments;
using GymManager.Domain.Sales.Errors;
using GymManager.Domain.Sales.Events;
using GymManager.SharedKernel.Auditing;
using GymManager.SharedKernel.Primitives;
using GymManager.SharedKernel.Results;

namespace GymManager.Domain.Sales;

/// <summary>A point-of-sale transaction for one or more products. Stock deduction is coordinated by the
/// application layer against the <c>Product</c> aggregate rather than referenced directly here.</summary>
public sealed class Sale : AggregateRoot<Guid>, IAuditableEntity
{
    private readonly List<SaleLine> _lines = [];
    private readonly List<SalePayment> _payments = [];

    private Sale()
    {
    }

    private Sale(Guid id, Guid branchId, Guid? memberId, Guid soldByUserId) : base(id)
    {
        BranchId = branchId;
        MemberId = memberId;
        SoldByUserId = soldByUserId;
        Status = SaleStatus.Completed;
        SoldOnUtc = DateTimeOffset.UtcNow;
    }

    public Guid BranchId { get; private set; }

    public Guid? MemberId { get; private set; }

    public Guid SoldByUserId { get; private set; }

    public SaleStatus Status { get; private set; }

    public Guid? PaymentId { get; private set; }

    public DateTimeOffset SoldOnUtc { get; private set; }

    public IReadOnlyCollection<SaleLine> Lines => _lines.AsReadOnly();

    public IReadOnlyCollection<SalePayment> Payments => _payments.AsReadOnly();

    public string Currency => _lines.Count > 0 ? _lines[0].UnitPrice.Currency : Money.DefaultCurrency;

    public Money TotalAmount => _lines.Aggregate(Money.Zero(Currency), (total, line) => total.Add(line.LineTotal));

    public DateTimeOffset CreatedOnUtc { get; private set; }

    public string? CreatedBy { get; private set; }

    public DateTimeOffset? ModifiedOnUtc { get; private set; }

    public string? ModifiedBy { get; private set; }

    public static Result<Sale> Create(Guid branchId, Guid? memberId, Guid soldByUserId, IReadOnlyCollection<(Guid ProductId, string ProductName, int Quantity, Money UnitPrice)> lines)
    {
        if (lines.Count == 0)
            return Result.Failure<Sale>(SaleErrors.NoLines);

        var sale = new Sale(Guid.NewGuid(), branchId, memberId, soldByUserId);

        foreach (var line in lines)
            sale._lines.Add(new SaleLine(line.ProductId, line.ProductName, line.Quantity, line.UnitPrice));

        sale.Raise(new SaleCompletedDomainEvent(sale.Id, branchId));

        return Result.Success(sale);
    }

    /// <summary>Records one payment allocation. A sale paid via a single method has exactly one allocation
    /// covering the full total; a split payment has several, whose amounts must sum to the total (enforced
    /// by the caller — the application layer knows the full set of allocations up front, unlike this
    /// aggregate which only sees them added one at a time).</summary>
    public SalePayment AddPayment(PaymentMethod method, Money amount, Guid paymentId, Guid? giftCardId = null)
    {
        var salePayment = new SalePayment(method, amount, paymentId, giftCardId);
        _payments.Add(salePayment);
        PaymentId ??= paymentId;
        return salePayment;
    }

    /// <summary>Refunds the sale in full — including any quantity still outstanding on a sale that was
    /// already <see cref="SaleStatus.PartiallyRefunded"/>, since that state means only some lines were
    /// refunded, not that the sale has been fully settled yet.</summary>
    public Result Refund()
    {
        if (Status == SaleStatus.Refunded)
            return Result.Failure(SaleErrors.AlreadyRefunded);

        Status = SaleStatus.Refunded;
        foreach (var line in _lines.Where(l => l.RemainingQuantity > 0))
            line.RefundQuantity(line.RemainingQuantity);

        Raise(new SaleRefundedDomainEvent(Id));

        return Result.Success();
    }

    /// <summary>Refunds a specific quantity of one line — a partial refund, or the "return" half of an
    /// exchange (the application layer creates a new <see cref="Sale"/> for whatever replaces it). Moves the
    /// sale to <see cref="SaleStatus.PartiallyRefunded"/>, or fully to <see cref="SaleStatus.Refunded"/> if
    /// every line ends up completely refunded.</summary>
    public Result<Money> RefundLine(Guid lineId, int quantity)
    {
        if (Status == SaleStatus.Refunded)
            return Result.Failure<Money>(SaleErrors.AlreadyRefunded);

        var line = _lines.FirstOrDefault(l => l.Id == lineId);
        if (line is null)
            return Result.Failure<Money>(SaleErrors.LineNotFound);

        var refundResult = line.RefundQuantity(quantity);
        if (refundResult.IsFailure)
            return refundResult;

        Status = _lines.All(l => l.RemainingQuantity == 0) ? SaleStatus.Refunded : SaleStatus.PartiallyRefunded;
        if (Status == SaleStatus.Refunded)
            Raise(new SaleRefundedDomainEvent(Id));

        return refundResult;
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
