using GymManager.SharedKernel.Auditing;
using GymManager.SharedKernel.Primitives;
using GymManager.SharedKernel.Results;

namespace GymManager.Domain.Identity;

/// <summary>
/// A named, assignable bundle of <see cref="Permissions"/> codes. System roles (<see cref="IsSystemRole"/>)
/// are seeded at deployment time and cannot be renamed or deleted, though their permission set may still evolve.
/// </summary>
public sealed class Role : AggregateRoot<Guid>, IAuditableEntity
{
    private readonly List<RolePermission> _permissions = [];

    private Role()
    {
        Name = string.Empty;
        Description = string.Empty;
    }

    private Role(Guid id, string name, string description, bool isSystemRole) : base(id)
    {
        Name = name;
        Description = description;
        IsSystemRole = isSystemRole;
    }

    public string Name { get; private set; }

    public string Description { get; private set; }

    public bool IsSystemRole { get; private set; }

    public IReadOnlyCollection<RolePermission> Permissions => _permissions.AsReadOnly();

    public DateTimeOffset CreatedOnUtc { get; private set; }

    public string? CreatedBy { get; private set; }

    public DateTimeOffset? ModifiedOnUtc { get; private set; }

    public string? ModifiedBy { get; private set; }

    public static Role Create(string name, string description, bool isSystemRole = false) =>
        new(Guid.NewGuid(), name.Trim(), description.Trim(), isSystemRole);

    public Result Rename(string name, string description)
    {
        if (IsSystemRole)
            return Result.Failure(Errors.RoleErrors.SystemRoleImmutable);

        Name = name.Trim();
        Description = description.Trim();
        return Result.Success();
    }

    public void GrantPermission(string permissionCode)
    {
        if (_permissions.Any(p => p.Code == permissionCode))
            return;

        _permissions.Add(new RolePermission(permissionCode));
    }

    public void RevokePermission(string permissionCode) =>
        _permissions.RemoveAll(p => p.Code == permissionCode);

    public void SetCreated(DateTimeOffset onUtc, string? by)
    {
        CreatedOnUtc = onUtc;
        CreatedBy = by;
    }

    public void SetModified(DateTimeOffset onUtc, string? by)
    {
        ModifiedOnUtc = onUtc;
        ModifiedBy = by;
    }
}
