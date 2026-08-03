using GymManager.SharedKernel.Primitives;

namespace GymManager.Domain.Products.Events;

public sealed record ProductStockLowDomainEvent(Guid ProductId, string Name, int RemainingQuantity, int ReorderThreshold) : IDomainEvent;
