using GymManager.SharedKernel.Cqrs;

namespace GymManager.Application.Sales.RefundSale;

public sealed record RefundSaleCommand(Guid SaleId) : ICommand;
