using System.Net;
using System.Net.Http.Json;
using GymManager.Domain.Identity;
using Xunit;

namespace GymManager.IntegrationTests;

public sealed class ProductsAndSalesControllerTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private sealed record ProductResponse(Guid Id, string Sku, int StockQuantity, int ReorderThreshold, bool IsLowStock, bool IsActive);

    private sealed record SaleResponse(Guid Id, string Status, decimal TotalAmount, Guid? PaymentId);

    private static object ProductRequest(Guid branchId, int initialStock = 10, int reorderThreshold = 2) => new
    {
        name = $"Product-{Guid.NewGuid():N}",
        description = "A test product",
        sku = $"SKU-{Guid.NewGuid():N}"[..12],
        category = 0, // Supplement
        price = 19.99m,
        costPrice = 9.99m,
        currency = "USD",
        branchId,
        initialStock,
        reorderThreshold,
    };

    private async Task<Guid> CreateProductAsync(HttpClient client, Guid branchId, int initialStock = 10, int reorderThreshold = 2)
    {
        var response = await client.PostAsJsonAsync("/api/v1/products", ProductRequest(branchId, initialStock, reorderThreshold));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ProductResponse>())!.Id;
    }

    [Fact]
    public async Task CreateProduct_Should_Return_Created_Product_With_Initial_Stock()
    {
        var client = await TestAuthHelper.CreateAuthorizedClientAsync(factory, Permissions.Products.Manage);

        var response = await client.PostAsJsonAsync("/api/v1/products", ProductRequest(Guid.NewGuid(), initialStock: 5, reorderThreshold: 10));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var product = await response.Content.ReadFromJsonAsync<ProductResponse>();
        Assert.Equal(5, product!.StockQuantity);
        Assert.True(product.IsLowStock);
    }

    [Fact]
    public async Task ReceiveStock_Should_Increase_StockQuantity()
    {
        var client = await TestAuthHelper.CreateAuthorizedClientAsync(
            factory, Permissions.Products.Manage, Permissions.Inventory.Manage, Permissions.Products.View);
        var productId = await CreateProductAsync(client, Guid.NewGuid(), initialStock: 10);

        var receiveResponse = await client.PostAsJsonAsync($"/api/v1/products/{productId}/receive-stock", new { quantity = 15 });
        Assert.Equal(HttpStatusCode.NoContent, receiveResponse.StatusCode);

        var listResponse = await client.GetAsync("/api/v1/products");
        var page = await listResponse.Content.ReadFromJsonAsync<PagedProducts>();
        Assert.Equal(25, page!.Items.Single(p => p.Id == productId).StockQuantity);
    }

    [Fact]
    public async Task ReceiveStock_With_Negative_Quantity_Should_Return_BadRequest()
    {
        var client = await TestAuthHelper.CreateAuthorizedClientAsync(factory, Permissions.Products.Manage, Permissions.Inventory.Manage);
        var productId = await CreateProductAsync(client, Guid.NewGuid());

        var response = await client.PostAsJsonAsync($"/api/v1/products/{productId}/receive-stock", new { quantity = -5 });

        Assert.False(response.IsSuccessStatusCode);
    }

    [Fact]
    public async Task CreateSale_Should_Deduct_Stock_And_Return_Completed_Sale()
    {
        var client = await TestAuthHelper.CreateAuthorizedClientAsync(
            factory, Permissions.Products.Manage, Permissions.Pos.Sell, Permissions.Products.View);
        var branchId = Guid.NewGuid();
        var productId = await CreateProductAsync(client, branchId, initialStock: 10);

        var saleResponse = await client.PostAsJsonAsync("/api/v1/sales", new
        {
            branchId,
            memberId = (Guid?)null,
            lines = new[] { new { productId, quantity = 3 } },
            paymentMethod = 0, // Cash
        });

        Assert.Equal(HttpStatusCode.OK, saleResponse.StatusCode);
        var sale = await saleResponse.Content.ReadFromJsonAsync<SaleResponse>();
        Assert.Equal("Completed", sale!.Status);
        Assert.NotNull(sale.PaymentId);
        Assert.Equal(59.97m, sale.TotalAmount);

        var listResponse = await client.GetAsync("/api/v1/products");
        var page = await listResponse.Content.ReadFromJsonAsync<PagedProducts>();
        Assert.Equal(7, page!.Items.Single(p => p.Id == productId).StockQuantity);
    }

    [Fact]
    public async Task CreateSale_With_Insufficient_Stock_Should_Fail()
    {
        var client = await TestAuthHelper.CreateAuthorizedClientAsync(factory, Permissions.Products.Manage, Permissions.Pos.Sell);
        var branchId = Guid.NewGuid();
        var productId = await CreateProductAsync(client, branchId, initialStock: 2);

        var saleResponse = await client.PostAsJsonAsync("/api/v1/sales", new
        {
            branchId,
            memberId = (Guid?)null,
            lines = new[] { new { productId, quantity = 5 } },
            paymentMethod = 0,
        });

        Assert.False(saleResponse.IsSuccessStatusCode);
    }

    [Fact]
    public async Task RefundSale_Should_Mark_Sale_Refunded()
    {
        var client = await TestAuthHelper.CreateAuthorizedClientAsync(
            factory, Permissions.Products.Manage, Permissions.Pos.Sell, Permissions.Payments.Refund, Permissions.Payments.View);
        var branchId = Guid.NewGuid();
        var productId = await CreateProductAsync(client, branchId, initialStock: 10);

        var saleResponse = await client.PostAsJsonAsync("/api/v1/sales", new
        {
            branchId,
            memberId = (Guid?)null,
            lines = new[] { new { productId, quantity = 1 } },
            paymentMethod = 0,
        });
        var sale = await saleResponse.Content.ReadFromJsonAsync<SaleResponse>();

        var refundResponse = await client.PostAsync($"/api/v1/sales/{sale!.Id}/refund", content: null);
        Assert.Equal(HttpStatusCode.NoContent, refundResponse.StatusCode);

        var listResponse = await client.GetAsync($"/api/v1/sales?branchId={branchId}");
        listResponse.EnsureSuccessStatusCode();
        var page = await listResponse.Content.ReadFromJsonAsync<PagedSales>();
        Assert.Equal("Refunded", page!.Items.Single(s => s.Id == sale.Id).Status);
    }

    private sealed record PagedProducts(IReadOnlyList<ProductResponse> Items);

    private sealed record PagedSales(IReadOnlyList<SaleResponse> Items);
}
