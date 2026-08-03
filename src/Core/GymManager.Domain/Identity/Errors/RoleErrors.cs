using GymManager.SharedKernel.Results;

namespace GymManager.Domain.Identity.Errors;

/// <summary>Catalog of expected, domain-relevant failures for the <see cref="Role"/> aggregate.</summary>
public static class RoleErrors
{
    public static readonly Error NotFound = Error.NotFound("Role.NotFound", "The role was not found.");

    public static Error NameAlreadyInUse(string name) =>
        Error.Conflict("Role.NameAlreadyInUse", $"A role named '{name}' already exists.");

    public static readonly Error SystemRoleImmutable = Error.Forbidden("Role.SystemRoleImmutable", "Built-in system roles cannot be modified or deleted.");
}
