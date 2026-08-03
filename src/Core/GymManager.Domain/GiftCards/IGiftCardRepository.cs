using GymManager.Domain.Abstractions;

namespace GymManager.Domain.GiftCards;

public interface IGiftCardRepository : IRepository<GiftCard, Guid>
{
    Task<GiftCard?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);

    Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken = default);
}
