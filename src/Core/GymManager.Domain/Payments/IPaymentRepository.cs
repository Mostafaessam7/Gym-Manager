using GymManager.Domain.Abstractions;

namespace GymManager.Domain.Payments;

public interface IPaymentRepository : IRepository<Payment, Guid>
{
    Task<Payment?> GetByGatewayReferenceIdAsync(string gatewayReferenceId, CancellationToken cancellationToken = default);
}
