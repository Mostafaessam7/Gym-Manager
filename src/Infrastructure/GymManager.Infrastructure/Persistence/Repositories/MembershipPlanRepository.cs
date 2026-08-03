using GymManager.Domain.Memberships;
using Microsoft.EntityFrameworkCore;

namespace GymManager.Infrastructure.Persistence.Repositories;

internal sealed class MembershipPlanRepository(GymManagerDbContext dbContext) : IMembershipPlanRepository
{
    public Task<MembershipPlan?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.MembershipPlans.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public Task<bool> NameExistsAsync(string name, CancellationToken cancellationToken = default) =>
        dbContext.MembershipPlans.AnyAsync(p => p.Name == name, cancellationToken);

    public void Add(MembershipPlan aggregate) => dbContext.MembershipPlans.Add(aggregate);

    public void Update(MembershipPlan aggregate) => dbContext.MembershipPlans.Update(aggregate);

    public void Remove(MembershipPlan aggregate) => dbContext.MembershipPlans.Remove(aggregate);
}
