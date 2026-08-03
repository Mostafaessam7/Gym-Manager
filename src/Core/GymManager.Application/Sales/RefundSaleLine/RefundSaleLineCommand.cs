using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;

namespace GymManager.Application.Sales.RefundSaleLine;

/// <summary>Refunds a specific quantity of one line on a sale — a partial refund, or the "return" half of an
/// exchange (create a new sale for whatever replaces it).</summary>
public sealed record RefundSaleLineCommand(Guid SaleId, Guid LineId, int Quantity) : ICommand<Result<decimal>>;
