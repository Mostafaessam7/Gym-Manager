using Asp.Versioning;
using GymManager.Api.Authorization;
using GymManager.Api.Extensions;
using GymManager.Application.Invoices.Contracts;
using GymManager.Application.Invoices.CreateInvoice;
using GymManager.Application.Invoices.GetInvoiceById;
using GymManager.Application.Invoices.GetInvoices;
using GymManager.Application.Invoices.IssueInvoice;
using GymManager.Application.Invoices.MarkInvoicePaid;
using GymManager.Application.Invoices.VoidInvoice;
using GymManager.Domain.Identity;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Pagination;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymManager.Api.Controllers.V1;

/// <summary>Line-itemized invoices for a member — draft, issue, mark-paid, and void — separate from the
/// simpler one-shot <c>PaymentsController</c> record.</summary>
[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/invoices")]
public sealed class InvoicesController(IDispatcher dispatcher) : ControllerBase
{
    public sealed record CreateInvoiceRequest(Guid MemberId, Guid BranchId, DateTimeOffset DueOnUtc, string Currency, IReadOnlyCollection<InvoiceLineRequest> Lines);

    public sealed record MarkPaidRequest(Guid PaymentId);

    [HttpGet]
    [HasPermission(Permissions.Invoices.View)]
    public async Task<IActionResult> GetInvoices(
        [FromQuery] PaginationParameters pagination, [FromQuery] Guid? branchId, [FromQuery] Guid? memberId,
        [FromQuery] string? status, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(new GetInvoicesQuery(pagination, branchId, memberId, status), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [HasPermission(Permissions.Invoices.View)]
    public async Task<IActionResult> GetInvoiceById(Guid id, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(new GetInvoiceByIdQuery(id), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPost]
    [HasPermission(Permissions.Invoices.Manage)]
    public async Task<IActionResult> CreateInvoice(CreateInvoiceRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateInvoiceCommand(request.MemberId, request.BranchId, request.DueOnUtc, request.Currency, request.Lines);
        var result = await dispatcher.Send(command, cancellationToken);

        return result.IsSuccess
            ? CreatedAtAction(nameof(GetInvoiceById), new { id = result.Value.Id }, result.Value)
            : result.ToProblemDetails();
    }

    [HttpPost("{id:guid}/issue")]
    [HasPermission(Permissions.Invoices.Manage)]
    public async Task<IActionResult> IssueInvoice(Guid id, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(new IssueInvoiceCommand(id), cancellationToken);
        return result.IsSuccess ? NoContent() : result.ToProblemDetails();
    }

    [HttpPost("{id:guid}/mark-paid")]
    [HasPermission(Permissions.Invoices.Manage)]
    public async Task<IActionResult> MarkPaid(Guid id, MarkPaidRequest request, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(new MarkInvoicePaidCommand(id, request.PaymentId), cancellationToken);
        return result.IsSuccess ? NoContent() : result.ToProblemDetails();
    }

    [HttpPost("{id:guid}/void")]
    [HasPermission(Permissions.Invoices.Manage)]
    public async Task<IActionResult> VoidInvoice(Guid id, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(new VoidInvoiceCommand(id), cancellationToken);
        return result.IsSuccess ? NoContent() : result.ToProblemDetails();
    }
}
