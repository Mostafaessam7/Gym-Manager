using GymManager.SharedKernel.Results;

namespace GymManager.Domain.Products.Errors;

public static class ProductErrors
{
    public static readonly Error NotFound = Error.NotFound("Product.NotFound", "The product was not found.");

    public static Error SkuAlreadyInUse(string sku) =>
        Error.Conflict("Product.SkuAlreadyInUse", $"A product with SKU '{sku}' already exists.");

    public static readonly Error InsufficientStock = Error.Conflict("Product.InsufficientStock", "There is not enough stock to complete this operation.");

    public static readonly Error InvalidQuantity = Error.Validation("Product.InvalidQuantity", "Quantity must be a positive number.");
}
