using GymManager.SharedKernel.Primitives;

namespace GymManager.Domain.Sales.Events;

public sealed record SaleCompletedDomainEvent(Guid SaleId, Guid BranchId) : IDomainEvent;

public sealed record SaleRefundedDomainEvent(Guid SaleId) : IDomainEvent;
