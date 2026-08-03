namespace GymManager.Domain.Payments;

public enum PaymentMethod
{
    Cash = 0,
    Card = 1,
    BankTransfer = 2,
    Other = 3,
    GiftCard = 4,
}

public enum PaymentStatus
{
    Pending = 0,
    Completed = 1,
    Refunded = 2,
    Failed = 3,
}

/// <summary>What a payment was collected for, used to trace it back to the originating record.</summary>
public enum PaymentReferenceType
{
    MembershipPurchase = 0,
    MembershipRenewal = 1,
    ClassBooking = 2,
    ProductSale = 3,
    Other = 4,
}
