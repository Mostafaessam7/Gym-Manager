using GymManager.Domain.Common;
using GymManager.Domain.GiftCards.Errors;
using GymManager.SharedKernel.Auditing;
using GymManager.SharedKernel.Primitives;
using GymManager.SharedKernel.Results;

namespace GymManager.Domain.GiftCards;

/// <summary>A prepaid store-credit card redeemable against sales, identified by a unique human-typeable
/// code rather than its <see cref="Entity{TId}.Id"/>.</summary>
public sealed class GiftCard : AggregateRoot<Guid>, IAuditableEntity
{
    private readonly List<GiftCardTransaction> _transactions = [];

    private GiftCard()
    {
        Code = string.Empty;
        InitialBalance = null!;
        CurrentBalance = null!;
    }

    private GiftCard(Guid id, string code, Money initialBalance, Guid? issuedToMemberId, DateTimeOffset? expiresOnUtc)
        : base(id)
    {
        Code = code;
        InitialBalance = initialBalance;
        // A distinct Money instance, not the same reference as InitialBalance — EF Core's change tracker
        // requires each owned-type navigation to have its own instance, even when the values start out equal.
        CurrentBalance = Money.Create(initialBalance.Amount, initialBalance.Currency).Value;
        IssuedToMemberId = issuedToMemberId;
        ExpiresOnUtc = expiresOnUtc;
        IsActive = true;
    }

    public string Code { get; private set; }

    public Money InitialBalance { get; private set; }

    public Money CurrentBalance { get; private set; }

    public Guid? IssuedToMemberId { get; private set; }

    public DateTimeOffset? ExpiresOnUtc { get; private set; }

    public bool IsActive { get; private set; }

    public IReadOnlyCollection<GiftCardTransaction> Transactions => _transactions.AsReadOnly();

    public DateTimeOffset CreatedOnUtc { get; private set; }

    public string? CreatedBy { get; private set; }

    public DateTimeOffset? ModifiedOnUtc { get; private set; }

    public string? ModifiedBy { get; private set; }

    public static GiftCard Issue(string code, Money initialBalance, Guid? issuedToMemberId, DateTimeOffset? expiresOnUtc)
    {
        var giftCard = new GiftCard(Guid.NewGuid(), code.Trim().ToUpperInvariant(), initialBalance, issuedToMemberId, expiresOnUtc);
        giftCard._transactions.Add(new GiftCardTransaction(GiftCardTransactionType.Issued, initialBalance, null, "Initial issuance"));
        return giftCard;
    }

    public Result Redeem(Money amount, Guid? referenceSaleId, DateTimeOffset utcNow)
    {
        if (amount.Amount <= 0)
            return Result.Failure(GiftCardErrors.InvalidAmount);

        if (!IsActive)
            return Result.Failure(GiftCardErrors.Inactive);

        if (ExpiresOnUtc is { } expiresOnUtc && expiresOnUtc <= utcNow)
            return Result.Failure(GiftCardErrors.Expired);

        if (CurrentBalance.Amount < amount.Amount)
            return Result.Failure(GiftCardErrors.InsufficientBalance);

        CurrentBalance = CurrentBalance.Subtract(amount);
        _transactions.Add(new GiftCardTransaction(GiftCardTransactionType.Redeemed, amount, referenceSaleId, null));

        return Result.Success();
    }

    public Result Reload(Money amount)
    {
        if (amount.Amount <= 0)
            return Result.Failure(GiftCardErrors.InvalidAmount);

        CurrentBalance = CurrentBalance.Add(amount);
        _transactions.Add(new GiftCardTransaction(GiftCardTransactionType.Reloaded, amount, null, null));

        return Result.Success();
    }

    public void Deactivate() => IsActive = false;

    public void Reactivate() => IsActive = true;

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
