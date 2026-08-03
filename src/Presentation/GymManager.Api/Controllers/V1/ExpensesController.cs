using Asp.Versioning;
using GymManager.Api.Authorization;
using GymManager.Api.Extensions;
using GymManager.Application.Expenses.DeleteExpense;
using GymManager.Application.Expenses.GetExpenses;
using GymManager.Application.Expenses.RecordExpense;
using GymManager.Application.Expenses.UpdateExpense;
using GymManager.Domain.Expenses;
using GymManager.Domain.Identity;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Pagination;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymManager.Api.Controllers.V1;

/// <summary>Operating expenses recorded per branch, categorized for the profit-and-loss report.</summary>
[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/expenses")]
public sealed class ExpensesController(IDispatcher dispatcher) : ControllerBase
{
    public sealed record ExpenseRequest(
        Guid BranchId, ExpenseCategory Category, string Description, decimal Amount, string Currency,
        DateOnly ExpenseDate, string PaidTo, string? ReceiptUrl);

    [HttpGet]
    [HasPermission(Permissions.Expenses.View)]
    public async Task<IActionResult> GetExpenses(
        [FromQuery] PaginationParameters pagination, [FromQuery] Guid? branchId, [FromQuery] string? category,
        [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(new GetExpensesQuery(pagination, branchId, category, from, to), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [HasPermission(Permissions.Expenses.Manage)]
    public async Task<IActionResult> RecordExpense(ExpenseRequest request, CancellationToken cancellationToken)
    {
        var command = new RecordExpenseCommand(
            request.BranchId, request.Category, request.Description, request.Amount, request.Currency,
            request.ExpenseDate, request.PaidTo, request.ReceiptUrl);
        var result = await dispatcher.Send(command, cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPut("{id:guid}")]
    [HasPermission(Permissions.Expenses.Manage)]
    public async Task<IActionResult> UpdateExpense(Guid id, ExpenseRequest request, CancellationToken cancellationToken)
    {
        var command = new UpdateExpenseCommand(
            id, request.Category, request.Description, request.Amount, request.Currency, request.ExpenseDate, request.PaidTo, request.ReceiptUrl);
        var result = await dispatcher.Send(command, cancellationToken);

        return result.IsSuccess ? NoContent() : result.ToProblemDetails();
    }

    [HttpDelete("{id:guid}")]
    [HasPermission(Permissions.Expenses.Manage)]
    public async Task<IActionResult> DeleteExpense(Guid id, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(new DeleteExpenseCommand(id), cancellationToken);
        return result.IsSuccess ? NoContent() : result.ToProblemDetails();
    }
}
