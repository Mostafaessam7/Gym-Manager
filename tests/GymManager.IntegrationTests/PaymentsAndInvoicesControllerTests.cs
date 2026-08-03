using System.Net;
using System.Net.Http.Json;
using GymManager.Domain.Identity;
using Xunit;

namespace GymManager.IntegrationTests;

public sealed class PaymentsAndInvoicesControllerTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private sealed record PaymentResponse(Guid Id, Guid MemberId, decimal Amount, string Status);

    private sealed record InvoiceResponse(Guid Id, string InvoiceNumber, string Status, decimal TotalAmount, Guid? PaymentId);

    private static object RecordPaymentRequest(Guid memberId, Guid branchId, decimal amount = 49.99m) => new
    {
        memberId,
        branchId,
        amount,
        currency = "USD",
        method = 0, // Cash
        referenceType = 4, // Other
        referenceId = (Guid?)null,
    };

    [Fact]
    public async Task RecordPayment_Should_Return_Completed_Payment()
    {
        var client = await TestAuthHelper.CreateAuthorizedClientAsync(factory, Permissions.Payments.Process);

        var response = await client.PostAsJsonAsync("/api/v1/payments", RecordPaymentRequest(Guid.NewGuid(), Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payment = await response.Content.ReadFromJsonAsync<PaymentResponse>();
        Assert.Equal("Completed", payment!.Status);
    }

    [Fact]
    public async Task RecordPayment_Without_Permission_Should_Return_Forbidden()
    {
        var client = await TestAuthHelper.CreateAuthorizedClientAsync(factory, Permissions.Payments.View);

        var response = await client.PostAsJsonAsync("/api/v1/payments", RecordPaymentRequest(Guid.NewGuid(), Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task RefundPayment_Should_Mark_Payment_As_Refunded()
    {
        var client = await TestAuthHelper.CreateAuthorizedClientAsync(
            factory, Permissions.Payments.Process, Permissions.Payments.Refund, Permissions.Payments.View);

        var recordResponse = await client.PostAsJsonAsync("/api/v1/payments", RecordPaymentRequest(Guid.NewGuid(), Guid.NewGuid()));
        var payment = await recordResponse.Content.ReadFromJsonAsync<PaymentResponse>();

        var refundResponse = await client.PostAsync($"/api/v1/payments/{payment!.Id}/refund", content: null);
        Assert.Equal(HttpStatusCode.NoContent, refundResponse.StatusCode);

        var listResponse = await client.GetAsync($"/api/v1/payments?memberId={payment.MemberId}");
        listResponse.EnsureSuccessStatusCode();
        var page = await listResponse.Content.ReadFromJsonAsync<PagedPayments>();
        Assert.Equal("Refunded", page!.Items.Single(p => p.Id == payment.Id).Status);
    }

    [Fact]
    public async Task RefundPayment_Twice_Should_Fail_Second_Time()
    {
        var client = await TestAuthHelper.CreateAuthorizedClientAsync(factory, Permissions.Payments.Process, Permissions.Payments.Refund);

        var recordResponse = await client.PostAsJsonAsync("/api/v1/payments", RecordPaymentRequest(Guid.NewGuid(), Guid.NewGuid()));
        var payment = await recordResponse.Content.ReadFromJsonAsync<PaymentResponse>();

        await client.PostAsync($"/api/v1/payments/{payment!.Id}/refund", content: null);
        var secondRefund = await client.PostAsync($"/api/v1/payments/{payment.Id}/refund", content: null);

        Assert.False(secondRefund.IsSuccessStatusCode);
    }

    [Fact]
    public async Task CreateInvoice_Issue_Then_MarkPaid_Should_Transition_Status()
    {
        var client = await TestAuthHelper.CreateAuthorizedClientAsync(
            factory, Permissions.Invoices.Manage, Permissions.Invoices.View, Permissions.Payments.Process);

        var memberId = Guid.NewGuid();
        var branchId = Guid.NewGuid();

        var createResponse = await client.PostAsJsonAsync("/api/v1/invoices", new
        {
            memberId,
            branchId,
            dueOnUtc = DateTimeOffset.UtcNow.AddDays(14),
            currency = "USD",
            lines = new[] { new { description = "Monthly membership", quantity = 1, unitPrice = 49.99m } },
        });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var invoice = await createResponse.Content.ReadFromJsonAsync<InvoiceResponse>();
        Assert.Equal("Draft", invoice!.Status);
        Assert.Equal(49.99m, invoice.TotalAmount);

        var issueResponse = await client.PostAsync($"/api/v1/invoices/{invoice.Id}/issue", content: null);
        Assert.Equal(HttpStatusCode.NoContent, issueResponse.StatusCode);

        var paymentResponse = await client.PostAsJsonAsync("/api/v1/payments", RecordPaymentRequest(memberId, branchId, invoice.TotalAmount));
        var payment = await paymentResponse.Content.ReadFromJsonAsync<PaymentResponse>();

        var markPaidResponse = await client.PostAsJsonAsync($"/api/v1/invoices/{invoice.Id}/mark-paid", new { paymentId = payment!.Id });
        Assert.Equal(HttpStatusCode.NoContent, markPaidResponse.StatusCode);

        var getResponse = await client.GetAsync($"/api/v1/invoices/{invoice.Id}");
        var updated = await getResponse.Content.ReadFromJsonAsync<InvoiceResponse>();
        Assert.Equal("Paid", updated!.Status);
        Assert.Equal(payment.Id, updated.PaymentId);
    }

    [Fact]
    public async Task CreateInvoice_With_No_Lines_Should_Return_BadRequest()
    {
        var client = await TestAuthHelper.CreateAuthorizedClientAsync(factory, Permissions.Invoices.Manage);

        var response = await client.PostAsJsonAsync("/api/v1/invoices", new
        {
            memberId = Guid.NewGuid(),
            branchId = Guid.NewGuid(),
            dueOnUtc = DateTimeOffset.UtcNow.AddDays(14),
            currency = "USD",
            lines = Array.Empty<object>(),
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task VoidInvoice_After_Paid_Should_Fail()
    {
        var client = await TestAuthHelper.CreateAuthorizedClientAsync(
            factory, Permissions.Invoices.Manage, Permissions.Payments.Process);

        var memberId = Guid.NewGuid();
        var branchId = Guid.NewGuid();

        var createResponse = await client.PostAsJsonAsync("/api/v1/invoices", new
        {
            memberId,
            branchId,
            dueOnUtc = DateTimeOffset.UtcNow.AddDays(14),
            currency = "USD",
            lines = new[] { new { description = "Monthly membership", quantity = 1, unitPrice = 49.99m } },
        });
        var invoice = await createResponse.Content.ReadFromJsonAsync<InvoiceResponse>();

        await client.PostAsync($"/api/v1/invoices/{invoice!.Id}/issue", content: null);

        var paymentResponse = await client.PostAsJsonAsync("/api/v1/payments", RecordPaymentRequest(memberId, branchId, invoice.TotalAmount));
        var payment = await paymentResponse.Content.ReadFromJsonAsync<PaymentResponse>();
        await client.PostAsJsonAsync($"/api/v1/invoices/{invoice.Id}/mark-paid", new { paymentId = payment!.Id });

        var voidResponse = await client.PostAsync($"/api/v1/invoices/{invoice.Id}/void", content: null);

        Assert.False(voidResponse.IsSuccessStatusCode);
    }

    private sealed record PagedPayments(IReadOnlyList<PaymentResponse> Items);
}
