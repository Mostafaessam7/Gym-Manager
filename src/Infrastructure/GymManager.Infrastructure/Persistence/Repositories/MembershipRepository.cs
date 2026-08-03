using GymManager.Domain.Memberships;
using Microsoft.EntityFrameworkCore;

namespace GymManager.Infrastructure.Persistence.Repositories;

internal sealed class MembershipRepository(GymManagerDbContext dbContext) : IMembershipRepository
{
    // Renewals is an owned collection mapped to its own table; EF Core never auto-includes those, so the
    // Renew() history and Membership.Renewals reads would silently see an empty list without this.
    private IQueryable<Membership> MembershipsWithRenewals => dbContext.Memberships.Include(m => m.Renewals);

    public Task<Membership?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        MembershipsWithRenewals.FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

    public Task<Membership?> GetActiveByMemberIdAsync(Guid memberId, CancellationToken cancellationToken = default) =>
        dbContext.Memberships
            .Where(m => m.MemberId == memberId && m.Status == MembershipStatus.Active)
            .OrderByDescending(m => m.EndDate)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<Membership>> GetByMemberIdAsync(Guid memberId, CancellationToken cancellationToken = default) =>
        await MembershipsWithRenewals
            .Where(m => m.MemberId == memberId)
            .OrderByDescending(m => m.StartDate)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Membership>> GetActiveMembershipsExpiringBetweenAsync(
        DateOnly from, DateOnly to, CancellationToken cancellationToken = default) =>
        await dbContext.Memberships
            .Where(m => m.Status == MembershipStatus.Active && m.EndDate >= from && m.EndDate <= to)
            .OrderBy(m => m.EndDate)
            .ToListAsync(cancellationToken);

    public void Add(Membership aggregate) => dbContext.Memberships.Add(aggregate);

    public void Update(Membership aggregate) => dbContext.Memberships.Update(aggregate);

    public void Remove(Membership aggregate) => dbContext.Memberships.Remove(aggregate);
}
