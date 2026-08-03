using System.Net;
using System.Net.Http.Json;
using GymManager.Domain.Identity;
using Xunit;

namespace GymManager.IntegrationTests;

/// <summary>Covers the POS depth additions: gift cards (issue/redeem/reload/deactivate), split payments on a
/// sale, and partial (line-level) refunds.</summary>
public sealed class PosDepthTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private sealed record ProductResponse(Guid Id, int StockQuantity);

    private sealed record SaleLineResponse(Guid Id, Guid ProductId, int Quantity, int RefundedQuantity, int RemainingQuantity);

    private sealed record SalePaymentResponse(Guid Id, string Method, decimal Amount, Guid PaymentId, Guid? GiftCardId);

    private sealed record SaleResponse(
        Guid Id, string Status, decimal TotalAmount, IReadOnlyList<SaleLineResponse> Lines, IReadOnlyList<SalePaymentResponse> Payments);

    private sealed record GiftCardTransactionResponse(Guid Id, string Type, decimal Amount);

    private sealed record GiftCardResponse(
        Guid Id, string Code, decimal InitialBalance, decimal CurrentBalance, bool IsActive, IReadOnlyList<GiftCardTransactionResponse> Transactions);

    private sealed record PagedProducts(IReadOnlyList<ProductResponse> Items);

    private static object ProductRequest(Guid branchId, int initialStock = 10) => new
    {
        name = $"Product-{Guid.NewGuid():N}",
        description = "A test product",
        sku = $"SKU-{Guid.NewGuid():N}"[..12],
        category = 0,
        price = 20.00m,
        costPrice = 10.00m,
        currency = "USD",
        branchId,
        initialStock,
        reorderThreshold = 2,
    };

    private async Task<Guid> CreateProductAsync(HttpClient client, Guid branchId, int initialStock = 10)
    {
        var response = await client.PostAsJsonAsync("/api/v1/products", ProductRequest(branchId, initialStock));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ProductResponse>())!.Id;
    }

    [Fact]
    public async Task IssueGiftCard_Should_Return_A_New_Card_With_A_Generated_Code()
    {
        var client = await TestAuthHelper.CreateAuthorizedClientAsync(factory, Permissions.GiftCards.Manage, Permissions.GiftCards.View);

        var response = await client.PostAsJsonAsync(
            "/api/v1/gift-cards", new { initialBalance = 50m, code = (string?)null, issuedToMemberId = (Guid?)null, expiresOnUtc = (DateTimeOffset?)null });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var giftCard = await response.Content.ReadFromJsonAsync<GiftCardResponse>();
        Assert.StartsWith("GC-", giftCard!.Code);
        Assert.Equal(50m, giftCard.CurrentBalance);
        Assert.Single(giftCard.Transactions);
    }

    [Fact]
    public async Task IssueGiftCard_With_A_Duplicate_Code_Should_Return_Conflict()
    {
        var client = await TestAuthHelper.CreateAuthorizedClientAsync(factory, Permissions.GiftCards.Manage);
        var code = $"GC-DUP{Guid.NewGuid():N}"[..12];

        await client.PostAsJsonAsync("/api/v1/gift-cards", new { initialBalance = 50m, code, issuedToMemberId = (Guid?)null, expiresOnUtc = (DateTimeOffset?)null });
        var secondResponse = await client.PostAsJsonAsync(
            "/api/v1/gift-cards", new { initialBalance = 20m, code, issuedToMemberId = (Guid?)null, expiresOnUtc = (DateTimeOffset?)null });

        Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);
    }

    [Fact]
    public async Task ReloadGiftCard_Should_Increase_The_Current_Balance()
    {
        var client = await TestAuthHelper.CreateAuthorizedClientAsync(factory, Permissions.GiftCards.Manage, Permissions.GiftCards.View);
        var giftCard = await (await client.PostAsJsonAsync(
            "/api/v1/gift-cards", new { initialBalance = 50m, code = (string?)null, issuedToMemberId = (Guid?)null, expiresOnUtc = (DateTimeOffset?)null }))
            .Content.ReadFromJsonAsync<GiftCardResponse>();

        var reloadResponse = await client.PostAsJsonAsync($"/api/v1/gift-cards/{giftCard!.Id}/reload", new { amount = 25m });
        Assert.Equal(HttpStatusCode.NoContent, reloadResponse.StatusCode);

        var reloaded = await (await client.GetAsync($"/api/v1/gift-cards/{giftCard.Code}")).Content.ReadFromJsonAsync<GiftCardResponse>();
        Assert.Equal(75m, reloaded!.CurrentBalance);
    }

    [Fact]
    public async Task DeactivateGiftCard_Should_Prevent_It_Being_Redeemed_In_A_Sale()
    {
        var client = await TestAuthHelper.CreateAuthorizedClientAsync(
            factory, Permissions.GiftCards.Manage, Permissions.GiftCards.View, Permissions.Products.Manage, Permissions.Pos.Sell);
        var giftCard = await (await client.PostAsJsonAsync(
            "/api/v1/gift-cards", new { initialBalance = 100m, code = (string?)null, issuedToMemberId = (Guid?)null, expiresOnUtc = (DateTimeOffset?)null }))
            .Content.ReadFromJsonAsync<GiftCardResponse>();
        await client.PostAsync($"/api/v1/gift-cards/{giftCard!.Id}/deactivate", content: null);

        var branchId = Guid.NewGuid();
        var productId = await CreateProductAsync(client, branchId);

        var saleResponse = await client.PostAsJsonAsync("/api/v1/sales", new
        {
            branchId,
            memberId = (Guid?)null,
            lines = new[] { new { productId, quantity = 1 } },
            paymentMethod = 4, // GiftCard (ignored — SplitPayments takes precedence)
            splitPayments = new[] { new { method = 4, amount = 20.00m, giftCardCode = giftCard.Code } },
        });

        Assert.Equal(HttpStatusCode.Forbidden, saleResponse.StatusCode);
    }

    [Fact]
    public async Task CreateSale_Split_Between_Cash_And_GiftCard_Should_Redeem_The_GiftCard_Partially()
    {
        var client = await TestAuthHelper.CreateAuthorizedClientAsync(
            factory, Permissions.GiftCards.Manage, Permissions.GiftCards.View, Permissions.Products.Manage, Permissions.Pos.Sell);
        var giftCard = await (await client.PostAsJsonAsync(
            "/api/v1/gift-cards", new { initialBalance = 15m, code = (string?)null, issuedToMemberId = (Guid?)null, expiresOnUtc = (DateTimeOffset?)null }))
            .Content.ReadFromJsonAsync<GiftCardResponse>();

        var branchId = Guid.NewGuid();
        var productId = await CreateProductAsync(client, branchId); // price 20.00

        var saleResponse = await client.PostAsJsonAsync("/api/v1/sales", new
        {
            branchId,
            memberId = (Guid?)null,
            lines = new[] { new { productId, quantity = 1 } },
            paymentMethod = 0,
            splitPayments = new[]
            {
                new { method = 0, amount = 5.00m, giftCardCode = (string?)null }, // Cash
                new { method = 4, amount = 15.00m, giftCardCode = (string?)giftCard!.Code }, // GiftCard
            },
        });

        Assert.Equal(HttpStatusCode.OK, saleResponse.StatusCode);
        var sale = await saleResponse.Content.ReadFromJsonAsync<SaleResponse>();
        Assert.Equal(2, sale!.Payments.Count);
        Assert.Contains(sale.Payments, p => p.Method == "GiftCard" && p.Amount == 15.00m);

        var reloaded = await (await client.GetAsync($"/api/v1/gift-cards/{giftCard.Code}")).Content.ReadFromJsonAsync<GiftCardResponse>();
        Assert.Equal(0m, reloaded!.CurrentBalance);
    }

    [Fact]
    public async Task CreateSale_With_Mismatched_SplitPayment_Total_Should_Return_BadRequest()
    {
        var client = await TestAuthHelper.CreateAuthorizedClientAsync(factory, Permissions.Products.Manage, Permissions.Pos.Sell);
        var branchId = Guid.NewGuid();
        var productId = await CreateProductAsync(client, branchId);

        var saleResponse = await client.PostAsJsonAsync("/api/v1/sales", new
        {
            branchId,
            memberId = (Guid?)null,
            lines = new[] { new { productId, quantity = 1 } },
            paymentMethod = 0,
            splitPayments = new[] { new { method = 0, amount = 10.00m, giftCardCode = (string?)null } }, // sale total is 20.00
        });

        Assert.Equal(HttpStatusCode.BadRequest, saleResponse.StatusCode);
    }

    [Fact]
    public async Task RefundLine_Should_Restock_Only_The_Refunded_Quantity_And_PartiallyRefund_The_Sale()
    {
        var client = await TestAuthHelper.CreateAuthorizedClientAsync(
            factory, Permissions.Products.Manage, Permissions.Pos.Sell, Permissions.Payments.Refund,
            Permissions.Products.View, Permissions.Payments.View);
        var branchId = Guid.NewGuid();
        var productId = await CreateProductAsync(client, branchId, initialStock: 10);

        var saleResponse = await client.PostAsJsonAsync("/api/v1/sales", new
        {
            branchId,
            memberId = (Guid?)null,
            lines = new[] { new { productId, quantity = 3 } },
            paymentMethod = 0,
        });
        var sale = await saleResponse.Content.ReadFromJsonAsync<SaleResponse>();
        var line = sale!.Lines.Single();

        var refundResponse = await client.PostAsJsonAsync($"/api/v1/sales/{sale.Id}/refund-line", new { lineId = line.Id, quantity = 1 });
        Assert.Equal(HttpStatusCode.OK, refundResponse.StatusCode);

        var products = await (await client.GetAsync("/api/v1/products")).Content.ReadFromJsonAsync<PagedProducts>();
        Assert.Equal(8, products!.Items.Single(p => p.Id == productId).StockQuantity); // 10 - 3 + 1

        var salesPage = await (await client.GetAsync($"/api/v1/sales?branchId={branchId}"))
            .Content.ReadFromJsonAsync<PagedSalesResponse>();
        var reloadedSale = salesPage!.Items.Single(s => s.Id == sale.Id);
        Assert.Equal("PartiallyRefunded", reloadedSale.Status);
        Assert.Equal(2, reloadedSale.Lines.Single().RemainingQuantity);
    }

    [Fact]
    public async Task RefundLine_With_A_Quantity_Exceeding_Remaining_Should_Return_BadRequest()
    {
        var client = await TestAuthHelper.CreateAuthorizedClientAsync(
            factory, Permissions.Products.Manage, Permissions.Pos.Sell, Permissions.Payments.Refund);
        var branchId = Guid.NewGuid();
        var productId = await CreateProductAsync(client, branchId);

        var sale = await (await client.PostAsJsonAsync("/api/v1/sales", new
        {
            branchId, memberId = (Guid?)null, lines = new[] { new { productId, quantity = 2 } }, paymentMethod = 0,
        })).Content.ReadFromJsonAsync<SaleResponse>();
        var line = sale!.Lines.Single();

        var response = await client.PostAsJsonAsync($"/api/v1/sales/{sale.Id}/refund-line", new { lineId = line.Id, quantity = 5 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private sealed record PagedSalesResponse(IReadOnlyList<SaleResponse> Items);
}
