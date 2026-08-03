using GymManager.Domain.Settings;
using Xunit;

namespace GymManager.UnitTests.Settings;

public sealed class SettingTests
{
    [Fact]
    public void Create_Should_Trim_Key_And_Assign_BranchId()
    {
        var branchId = Guid.NewGuid();
        var setting = Setting.Create("  tax_rate  ", "0.08", "Sales tax rate", branchId);

        Assert.Equal("tax_rate", setting.Key);
        Assert.Equal(branchId, setting.BranchId);
    }

    [Fact]
    public void UpdateValue_Should_Replace_Value_And_Description()
    {
        var setting = Setting.Create("tax_rate", "0.08", "Old description", null);

        setting.UpdateValue("0.10", "New description");

        Assert.Equal("0.10", setting.Value);
        Assert.Equal("New description", setting.Description);
    }
}
