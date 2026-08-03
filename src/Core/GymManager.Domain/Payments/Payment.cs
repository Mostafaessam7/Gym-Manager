using GymManager.Domain.Common;
using GymManager.Domain.Payments.Errors;
using GymManager.Domain.Payments.Events;
using GymManager.SharedKernel.Auditing;
using GymManager.SharedKernel.Primitives;
using GymManager.SharedKernel.Results;

namespace GymManager.Domain.Payments;

/// <summary>A single monetary collection from a member, traceable to the record it was collected for.</summary>
public sealed class Payment : AggregateRoot<Guid>, IAuditableEntity
{
    private Payment()
    {
        Amount = null!;
    }

    private Payment(
        Guid id, Guid memberId, Guid branchId, Money amount, PaymentMethod method,
        PaymentReferenceType referenceType, Guid? referenceId, Guid? processedByUserId)
        : base(id)
    {
        MemberId = memberId;
        BranchId = branchId;
        Amount = amount;
        Method = method;
        ReferenceType = referenceType;
        ReferenceId = referenceId;
        ProcessedByUserId = processedByUserId;
        Status = PaymentStatus.Pending;
    }

    public Guid MemberId { get; private set; }

    public Guid BranchId { get; private set; }

    public Money Amount { get; private set; }

    public PaymentMethod Method { get; private set; }

    public PaymentStatus Status { get; private set; }

    public PaymentReferenceType ReferenceType { get; private set; }

    public Guid? ReferenceId { get; private set; }

    public Guid? ProcessedByUserId { get; private set; }

    public DateTimeOffset? CompletedOnUtc { get; private set; }

    public PaymentGatewayProvider GatewayProvider { get; private set; } = PaymentGatewayProvider.None;

    /// <summary>The gateway's own identifier for this payment (e.g. a Stripe PaymentIntent id) — needed to
    /// look the payment back up when its webhook arrives, since the gateway has no knowledge of our
    /// internal <c>Id</c>.</summary>
    public string? GatewayReferenceId { get; private set; }

    public DateTimeOffset CreatedOnUtc { get; private set; }

    public string? CreatedBy { get; private set; }

    public DateTimeOffset? ModifiedOnUtc { get; private set; }

    public string? ModifiedBy { get; private set; }

    public static Payment Create(
        Guid memberId, Guid branchId, Money amount, PaymentMethod method,
        PaymentReferenceType referenceType, Guid? referenceId, Guid? processedByUserId) =>
        new(Guid.NewGuid(), memberId, branchId, amount, method, referenceType, referenceId, processedByUserId);

    /// <summary>Records which gateway is collecting this payment and its reference id for that gateway.
    /// Called right after <see cref="Create"/> when the payment is being routed through a gateway rather
    /// than recorded as already-settled cash/manual payment.</summary>
    public void AttachGatewayReference(PaymentGatewayProvider provider, string gatewayReferenceId)
    {
        GatewayProvider = provider;
        GatewayReferenceId = gatewayReferenceId;
    }

    public Result Complete()
    {
        if (Status != PaymentStatus.Pending)
            return Result.Failure(PaymentErrors.NotPending);

        Status = PaymentStatus.Completed;
        CompletedOnUtc = DateTimeOffset.UtcNow;
        Raise(new PaymentCompletedDomainEvent(Id, MemberId, Amount.Amount, Amount.Currency));

        return Result.Success();
    }

    public Result Fail()
    {
        if (Status != PaymentStatus.Pending)
            return Result.Failure(PaymentErrors.NotPending);

        Status = PaymentStatus.Failed;
        Raise(new PaymentFailedDomainEvent(Id, MemberId));

        return Result.Success();
    }

    public Result Refund()
    {
        if (Status != PaymentStatus.Completed)
            return Result.Failure(PaymentErrors.NotCompleted);

        Status = PaymentStatus.Refunded;
        Raise(new PaymentRefundedDomainEvent(Id, MemberId));

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
