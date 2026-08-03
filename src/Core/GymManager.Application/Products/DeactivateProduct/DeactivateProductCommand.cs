using GymManager.SharedKernel.Cqrs;

namespace GymManager.Application.Products.DeactivateProduct;

public sealed record DeactivateProductCommand(Guid ProductId) : ICommand;
