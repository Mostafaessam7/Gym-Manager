using GymManager.Domain.Common;
using GymManager.Domain.Products;
using Xunit;

namespace GymManager.UnitTests.Products;

public sealed class ProductTests
{
    private static Product CreateProduct(int initialStock = 10, int reorderThreshold = 5) =>
        Product.Create(
            "Whey Protein", "Vanilla flavor", "SKU-001", ProductCategory.Supplement,
            Money.Create(29.99m).Value, Money.Create(15m).Value, Guid.NewGuid(), initialStock, reorderThreshold);

    [Fact]
    public void DeductStock_Should_Reduce_Quantity()
    {
        var product = CreateProduct();

        var result = product.DeductStock(3);

        Assert.True(result.IsSuccess);
        Assert.Equal(7, product.StockQuantity);
    }

    [Fact]
    public void DeductStock_Should_Fail_When_Insufficient()
    {
        var product = CreateProduct(initialStock: 2);

        var result = product.DeductStock(5);

        Assert.True(result.IsFailure);
        Assert.Equal("Product.InsufficientStock", result.Error.Code);
        Assert.Equal(2, product.StockQuantity);
    }

    [Fact]
    public void DeductStock_Should_Raise_LowStock_Event_When_Threshold_Crossed()
    {
        var product = CreateProduct(initialStock: 10, reorderThreshold: 5);

        product.DeductStock(6);

        Assert.Single(product.DomainEvents);
    }

    [Fact]
    public void ReceiveStock_Should_Fail_For_NonPositive_Quantity()
    {
        var product = CreateProduct();

        var result = product.ReceiveStock(0);

        Assert.True(result.IsFailure);
        Assert.Equal("Product.InvalidQuantity", result.Error.Code);
    }

    [Fact]
    public void IsLowStock_Should_Reflect_Threshold_Comparison()
    {
        var product = CreateProduct(initialStock: 5, reorderThreshold: 5);

        Assert.True(product.IsLowStock);
    }
}
