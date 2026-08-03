using GymManager.Domain.Abstractions;

namespace GymManager.Domain.Memberships;

public interface IMembershipPlanRepository : IRepository<MembershipPlan, Guid>
{
    Task<bool> NameExistsAsync(string name, CancellationToken cancellationToken = default);
}
