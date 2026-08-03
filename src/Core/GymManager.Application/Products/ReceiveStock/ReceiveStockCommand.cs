using GymManager.SharedKernel.Cqrs;

namespace GymManager.Application.Products.ReceiveStock;

public sealed record ReceiveStockCommand(Guid ProductId, int Quantity) : ICommand;
