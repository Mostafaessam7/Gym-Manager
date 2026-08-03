using GymManager.Domain.Common;
using GymManager.Domain.Products.Errors;
using GymManager.Domain.Products.Events;
using GymManager.SharedKernel.Auditing;
using GymManager.SharedKernel.Primitives;
using GymManager.SharedKernel.Results;

namespace GymManager.Domain.Products;

/// <summary>A sellable product or supplement, tracked with its own stock level. Inventory is modeled as a
/// property of the product rather than a separate aggregate since stock only ever changes alongside a sale
/// or a stock receipt — there is no independent inventory lifecycle to protect.</summary>
public sealed class Product : AggregateRoot<Guid>, IAuditableEntity, ISoftDeletableEntity
{
    private Product()
    {
        Name = string.Empty;
        Description = string.Empty;
        Sku = string.Empty;
        Price = null!;
        CostPrice = null!;
    }

    private Product(Guid id, string name, string description, string sku, ProductCategory category, Money price, Money costPrice, Guid branchId, int initialStock, int reorderThreshold)
        : base(id)
    {
        Name = name;
        Description = description;
        Sku = sku;
        Category = category;
        Price = price;
        CostPrice = costPrice;
        BranchId = branchId;
        StockQuantity = initialStock;
        ReorderThreshold = reorderThreshold;
        IsActive = true;
    }

    public string Name { get; private set; }

    public string Description { get; private set; }

    public string Sku { get; private set; }

    public ProductCategory Category { get; private set; }

    public Money Price { get; private set; }

    public Money CostPrice { get; private set; }

    public Guid BranchId { get; private set; }

    public int StockQuantity { get; private set; }

    public int ReorderThreshold { get; private set; }

    public bool IsActive { get; private set; }

    public bool IsLowStock => StockQuantity <= ReorderThreshold;

    public DateTimeOffset CreatedOnUtc { get; private set; }

    public string? CreatedBy { get; private set; }

    public DateTimeOffset? ModifiedOnUtc { get; private set; }

    public string? ModifiedBy { get; private set; }

    public bool IsDeleted { get; private set; }

    public DateTimeOffset? DeletedOnUtc { get; private set; }

    public string? DeletedBy { get; private set; }

    public static Product Create(
        string name, string description, string sku, ProductCategory category, Money price, Money costPrice,
        Guid branchId, int initialStock, int reorderThreshold) =>
        new(Guid.NewGuid(), name.Trim(), description.Trim(), sku.Trim().ToUpperInvariant(), category, price, costPrice, branchId, initialStock, reorderThreshold);

    public void Update(string name, string description, ProductCategory category, Money price, Money costPrice, int reorderThreshold)
    {
        Name = name.Trim();
        Description = description.Trim();
        Category = category;
        Price = price;
        CostPrice = costPrice;
        ReorderThreshold = reorderThreshold;
    }

    public Result ReceiveStock(int quantity)
    {
        if (quantity <= 0)
            return Result.Failure(ProductErrors.InvalidQuantity);

        StockQuantity += quantity;
        return Result.Success();
    }

    public Result DeductStock(int quantity)
    {
        if (quantity <= 0)
            return Result.Failure(ProductErrors.InvalidQuantity);

        if (StockQuantity < quantity)
            return Result.Failure(ProductErrors.InsufficientStock);

        StockQuantity -= quantity;

        if (IsLowStock)
            Raise(new ProductStockLowDomainEvent(Id, Name, StockQuantity, ReorderThreshold));

        return Result.Success();
    }

    public void Deactivate() => IsActive = false;

    public void Activate() => IsActive = true;

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

    public void Delete(DateTimeOffset onUtc, string? by)
    {
        IsDeleted = true;
        DeletedOnUtc = onUtc;
        DeletedBy = by;
    }

    public void Restore()
    {
        IsDeleted = false;
        DeletedOnUtc = null;
        DeletedBy = null;
    }
}
