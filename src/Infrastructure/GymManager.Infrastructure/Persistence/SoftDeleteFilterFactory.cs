using System.Linq.Expressions;
using GymManager.SharedKernel.Auditing;

namespace GymManager.Infrastructure.Persistence;

/// <summary>Builds the <c>e =&gt; !e.IsDeleted</c> global query filter expression for a soft-deletable entity type.</summary>
internal static class SoftDeleteFilterFactory
{
    public static LambdaExpression Build(Type entityType)
    {
        var parameter = Expression.Parameter(entityType, "entity");
        var property = Expression.Property(parameter, nameof(ISoftDeletableEntity.IsDeleted));
        var body = Expression.Not(property);

        return Expression.Lambda(body, parameter);
    }
}
