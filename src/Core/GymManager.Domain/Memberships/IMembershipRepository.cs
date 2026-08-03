using GymManager.Domain.Abstractions;

namespace GymManager.Domain.Memberships;

public interface IMembershipRepository : IRepository<Membership, Guid>
{
    Task<Membership?> GetActiveByMemberIdAsync(Guid memberId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Membership>> GetByMemberIdAsync(Guid memberId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Membership>> GetActiveMembershipsExpiringBetweenAsync(
        DateOnly from, DateOnly to, CancellationToken cancellationToken = default);
}
