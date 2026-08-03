using GymManager.Domain.GiftCards;
using Microsoft.EntityFrameworkCore;

namespace GymManager.Infrastructure.Persistence.Repositories;

internal sealed class GiftCardRepository(GymManagerDbContext dbContext) : IGiftCardRepository
{
    public Task<GiftCard?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.GiftCards.FirstOrDefaultAsync(g => g.Id == id, cancellationToken);

    public Task<GiftCard?> GetByCodeAsync(string code, CancellationToken cancellationToken = default) =>
        dbContext.GiftCards.FirstOrDefaultAsync(g => g.Code == code.Trim().ToUpper(), cancellationToken);

    public Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken = default) =>
        dbContext.GiftCards.AnyAsync(g => g.Code == code.Trim().ToUpper(), cancellationToken);

    public void Add(GiftCard aggregate) => dbContext.GiftCards.Add(aggregate);

    public void Update(GiftCard aggregate) => dbContext.GiftCards.Update(aggregate);

    public void Remove(GiftCard aggregate) => dbContext.GiftCards.Remove(aggregate);
}
