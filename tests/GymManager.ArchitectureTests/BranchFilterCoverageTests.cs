using System.Reflection;
using GymManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GymManager.ArchitectureTests;

/// <summary>
/// Pins which <see cref="DbSet{TEntity}"/> entities are covered by the global branch-isolation
/// filter, and which are not.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="BranchIsolationFilterFactory"/> builds a filter for any entity exposing a
/// <c>BranchId</c> property. An entity without one gets no filter at all — silently. That is not
/// automatically a bug: <c>Branch</c> and <c>Role</c> are genuinely global. But most of the
/// uncovered set is branch-scoped in reality and reachable only through a parent, so every query
/// against it has to re-derive the branch by hand. The dashboard does exactly that:
/// </para>
/// <code>
///     var branchMemberIds = readDb.Members.Where(m => m.BranchId == branchId).Select(m => m.Id);
///     memberships = memberships.Where(m => branchMemberIds.Contains(m.MemberId));
/// </code>
/// <para>
/// That is correct, and it is also the shape the global filter exists to make unnecessary — it
/// works only for as long as everyone remembers. This codebase has already shipped the
/// "someone forgot" bug twice, across 17 and 16 handlers (see
/// <see cref="BranchIsolationConventionTests"/>).
/// </para>
/// <para>
/// So this test does not demand the gap be closed — adding <c>BranchId</c> to these entities means
/// migrations and backfill, and is a decision rather than a cleanup. It pins the gap at its current
/// size. A new entity that is branch-scoped but lacks <c>BranchId</c> fails here, and closing one
/// by adding <c>BranchId</c> also fails here, so the list cannot drift in either direction without
/// someone looking at it.
/// </para>
/// </remarks>
public class BranchFilterCoverageTests
{
    /// <summary>
    /// Entities that are correctly outside branch isolation because they are genuinely global.
    /// </summary>
    private static readonly HashSet<string> GloballyScopedByDesign =
    [
        "Branch",  // the branch itself; filtering it by branch is circular
        "Role",    // identity roles are system-wide, not per-branch
    ];

    /// <summary>
    /// Entities that ARE branch-scoped in practice but carry no <c>BranchId</c>, so every query
    /// against them must scope by hand through the parent named beside it.
    ///
    /// This is tracked debt, not an approved design. Shrinking it is the goal; it must never grow
    /// without a deliberate decision.
    /// </summary>
    private static readonly HashSet<string> ScopedByHandThroughAParent =
    [
        "AuditLog",        // UserId
        "BodyMeasurement", // MemberId
        "Commission",      // UserId
        "GiftCard",        // IssuedToMemberId (nullable — an unissued card has no owner)
        "LeaveRequest",    // UserId
        "Membership",      // MemberId
        "Notification",    // RecipientUserId / RecipientMemberId
        "NutritionLog",    // MemberId
        "NutritionPlan",   // MemberId
        "WorkoutLog",      // MemberId
        "WorkoutPlan",     // MemberId
    ];

    [Fact]
    public void Every_DbSet_entity_is_either_branch_filtered_or_listed_here()
    {
        var uncovered = DbSetEntityTypes()
            .Where(t => t.GetProperty("BranchId") is null)
            .Select(t => t.Name)
            .ToHashSet();

        var accountedFor = new HashSet<string>(GloballyScopedByDesign);
        accountedFor.UnionWith(ScopedByHandThroughAParent);

        var unlisted = uncovered.Except(accountedFor).OrderBy(n => n).ToList();

        Assert.True(
            unlisted.Count == 0,
            $"""
             These entities are exposed as a DbSet, carry no BranchId, and are not listed in this
             test, so nothing scopes them to a branch automatically:

               {string.Join("\n  ", unlisted)}

             Decide which they are, then add them to the matching list:
               • genuinely global (like Branch or Role)  -> GloballyScopedByDesign
               • branch-scoped through a parent          -> ScopedByHandThroughAParent, and make
                                                            sure every query against it derives the
                                                            branch from that parent

             Preferably neither: give the entity a BranchId and it is filtered automatically.
             """);
    }

    [Fact]
    public void The_hand_scoped_list_does_not_name_entities_that_are_now_filtered()
    {
        // The other direction. If someone adds BranchId to one of these, it becomes covered by the
        // global filter and the entry here is stale -- a stale entry is worse than none, because it
        // claims manual scoping is still required and invites redundant, subtly different filtering
        // in new handlers.
        var nowFiltered = DbSetEntityTypes()
            .Where(t => t.GetProperty("BranchId") is not null)
            .Select(t => t.Name)
            .ToHashSet();

        var stale = ScopedByHandThroughAParent.Concat(GloballyScopedByDesign)
            .Where(nowFiltered.Contains)
            .OrderBy(n => n)
            .ToList();

        Assert.True(
            stale.Count == 0,
            $"""
             These entities now have a BranchId, so the global filter already covers them and they
             no longer need hand-scoping:

               {string.Join("\n  ", stale)}

             Remove them from the lists in this test, and check whether any handler is still
             scoping them by hand -- that filtering is now redundant.
             """);
    }

    /// <summary>
    /// Entity types reachable as a <c>DbSet</c>. Read from the context's own properties rather than
    /// the EF model, so this needs no database and stays an architecture test.
    /// </summary>
    private static IEnumerable<Type> DbSetEntityTypes() =>
        typeof(GymManagerDbContext)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.PropertyType.IsGenericType
                        && p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>))
            .Select(p => p.PropertyType.GetGenericArguments()[0]);
}
