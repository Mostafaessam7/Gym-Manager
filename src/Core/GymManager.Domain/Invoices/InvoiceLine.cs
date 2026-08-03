using GymManager.Domain.Common;
using GymManager.SharedKernel.Primitives;

namespace GymManager.Domain.Invoices;

/// <summary>A single billable line item on an <see cref="Invoice"/>.</summary>
public sealed class InvoiceLine : Entity<Guid>
{
    private InvoiceLine()
    {
        Description = string.Empty;
        UnitPrice = null!;
    }

    internal InvoiceLine(string description, int quantity, Money unitPrice) : base(Guid.NewGuid())
    {
        Description = description;
        Quantity = quantity;
        UnitPrice = unitPrice;
    }

    public string Description { get; private set; }

    public int Quantity { get; private set; }

    public Money UnitPrice { get; private set; }

    public Money LineTotal => Money.Create(UnitPrice.Amount * Quantity, UnitPrice.Currency).Value;
}
