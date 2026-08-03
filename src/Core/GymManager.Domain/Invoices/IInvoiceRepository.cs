using GymManager.Domain.Abstractions;

namespace GymManager.Domain.Invoices;

public interface IInvoiceRepository : IRepository<Invoice, Guid>
{
    Task<int> GetTotalCountAsync(CancellationToken cancellationToken = default);
}
