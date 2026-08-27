using GymManager.Domain.Abstractions;

namespace GymManager.Domain.Members;

public interface IMemberRepository : IRepository<Member, Guid>
{
    Task<Member?> GetByCheckInCodeAsync(string checkInCode, CancellationToken cancellationToken = default);

    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);

    Task<int> GetTotalCountAsync(CancellationToken cancellationToken = default);

    /// <summary>Resolves just the branch a member belongs to, bypassing every global query filter (branch
    /// isolation, soft-delete) — for handlers that fetch a *different* aggregate's owning member purely to
    /// authorize the caller against it (Membership/WorkoutPlan/NutritionPlan freeze/renew/update/etc.), never
    /// to expose the member itself. Unlike <see cref="IRepository{TAggregate,TId}.GetByIdAsync"/>, this must
    /// still find a member in another branch — otherwise the branch-isolation query filter silently hides the
    /// row, "member is null" stops meaning "doesn't exist" and starts meaning "exists in a branch I can't
    /// see," and every caller's own <c>if (member is not null) { EnsureCanAccess(...) }</c> guard becomes dead
    /// code that a branch-scoped caller can walk straight through.</summary>
    Task<Guid?> GetBranchIdForAuthorizationAsync(Guid memberId, CancellationToken cancellationToken = default);
}
