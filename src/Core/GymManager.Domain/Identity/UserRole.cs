using GymManager.SharedKernel.Primitives;

namespace GymManager.Domain.Identity;

/// <summary>Join entity recording that a <see cref="User"/> has been granted a <see cref="Role"/>.</summary>
public sealed class UserRole : Entity<Guid>
{
    private UserRole()
    {
    }

    internal UserRole(Guid roleId) : base(Guid.NewGuid())
    {
        RoleId = roleId;
        AssignedOnUtc = DateTimeOffset.UtcNow;
    }

    public Guid RoleId { get; private set; }

    public DateTimeOffset AssignedOnUtc { get; private set; }
}
