using GymManager.Domain.Identity;
using Xunit;

namespace GymManager.UnitTests.Identity;

public sealed class RoleTests
{
    [Fact]
    public void GrantPermission_Should_Not_Duplicate_Existing_Permission()
    {
        var role = Role.Create("Manager", "Operational management");

        role.GrantPermission(Permissions.Members.View);
        role.GrantPermission(Permissions.Members.View);

        Assert.Single(role.Permissions);
    }

    [Fact]
    public void RevokePermission_Should_Remove_Granted_Permission()
    {
        var role = Role.Create("Manager", "Operational management");
        role.GrantPermission(Permissions.Members.View);

        role.RevokePermission(Permissions.Members.View);

        Assert.Empty(role.Permissions);
    }

    [Fact]
    public void Rename_Should_Fail_For_System_Roles()
    {
        var role = Role.Create("Owner", "Full access", isSystemRole: true);

        var result = role.Rename("New Name", "New Description");

        Assert.True(result.IsFailure);
        Assert.Equal("Role.SystemRoleImmutable", result.Error.Code);
    }

    [Fact]
    public void Rename_Should_Succeed_For_Custom_Roles()
    {
        var role = Role.Create("Custom", "Custom role");

        var result = role.Rename("Renamed", "Updated description");

        Assert.True(result.IsSuccess);
        Assert.Equal("Renamed", role.Name);
    }
}
