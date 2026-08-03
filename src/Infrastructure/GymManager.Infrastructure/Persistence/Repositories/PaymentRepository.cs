using GymManager.Domain.Payments;
using Microsoft.EntityFrameworkCore;

namespace GymManager.Infrastructure.Persistence.Repositories;

internal sealed class PaymentRepository(GymManagerDbContext dbContext) : IPaymentRepository
{
    public Task<Payment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Payments.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public Task<Payment?> GetByGatewayReferenceIdAsync(string gatewayReferenceId, CancellationToken cancellationToken = default) =>
        dbContext.Payments.FirstOrDefaultAsync(p => p.GatewayReferenceId == gatewayReferenceId, cancellationToken);

    public void Add(Payment aggregate) => dbContext.Payments.Add(aggregate);

    public void Update(Payment aggregate) => dbContext.Payments.Update(aggregate);

    public void Remove(Payment aggregate) => dbContext.Payments.Remove(aggregate);
}
