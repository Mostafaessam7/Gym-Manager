using GymManager.SharedKernel.Primitives;

namespace GymManager.Domain.Identity;

/// <summary>A single permission code granted to a <see cref="Role"/>.</summary>
public sealed class RolePermission : ValueObject
{
    private RolePermission()
    {
        Code = string.Empty;
    }

    internal RolePermission(string code)
    {
        Code = code;
    }

    public string Code { get; private set; }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Code;
    }
}
