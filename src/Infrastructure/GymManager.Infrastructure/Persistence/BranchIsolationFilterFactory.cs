using System.Linq.Expressions;
using System.Reflection;

namespace GymManager.Infrastructure.Persistence;

/// <summary>
/// Builds a global query filter for any entity type that exposes a <c>BranchId</c> property (<see cref="Guid"/>
/// or nullable <see cref="Guid"/>), scoping every query against it to the caller's branch — a DB-layer safety net on
/// top of the existing per-handler <c>IBranchAccessGuard</c> checks, so a future handler that forgets to call
/// the guard still can't read or (via a subsequent fetch-then-modify) write another branch's data.
/// </summary>
/// <remarks>
/// A caller with no branch claim (Owner/HQ-level accounts — <see cref="GymManagerDbContext.CurrentBranchId"/>
/// is <see langword="null"/>) is never filtered. An entity whose own <c>BranchId</c> is <see langword="null"/>
/// (a global record — e.g. a branch-less <c>MembershipPlan</c> or <c>Setting</c>) is always visible, matching
/// the existing <c>ResolveFilter</c>/<c>EnsureCanAccess</c> convention used throughout the Application layer.
/// The filter references <see cref="GymManagerDbContext.CurrentBranchId"/> via a captured reference to the
/// executing context instance — the same "instance-based global query filter" pattern EF Core itself documents
/// for per-request/tenant filtering, re-evaluated against whichever <see cref="GymManagerDbContext"/> instance
/// actually runs the query, not baked in at model-build time.
/// </remarks>
internal static class BranchIsolationFilterFactory
{
    private static readonly PropertyInfo CurrentBranchIdProperty =
        typeof(GymManagerDbContext).GetProperty(
            nameof(GymManagerDbContext.CurrentBranchId), BindingFlags.Instance | BindingFlags.NonPublic)!;

    public static LambdaExpression? Build(Type entityType, GymManagerDbContext context)
    {
        var branchIdProperty = entityType.GetProperty("BranchId");
        if (branchIdProperty is null)
            return null;

        var isNullable = branchIdProperty.PropertyType == typeof(Guid?);
        if (!isNullable && branchIdProperty.PropertyType != typeof(Guid))
            return null;

        var parameter = Expression.Parameter(entityType, "entity");
        var entityBranchId = Expression.Property(parameter, branchIdProperty);

        var currentBranchId = Expression.Property(Expression.Constant(context), CurrentBranchIdProperty);
        var callerIsUnscoped = Expression.Not(Expression.Property(currentBranchId, nameof(Nullable<Guid>.HasValue)));

        Expression entityBranchIdAsNullable = isNullable ? entityBranchId : Expression.Convert(entityBranchId, typeof(Guid?));
        var matchesCallerBranch = Expression.Equal(entityBranchIdAsNullable, currentBranchId);

        Expression body = Expression.OrElse(callerIsUnscoped, matchesCallerBranch);

        if (isNullable)
        {
            var entityIsGlobal = Expression.Equal(entityBranchId, Expression.Constant(null, typeof(Guid?)));
            body = Expression.OrElse(body, entityIsGlobal);
        }

        return Expression.Lambda(body, parameter);
    }
}
