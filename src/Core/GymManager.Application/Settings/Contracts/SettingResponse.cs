using GymManager.Domain.Settings;

namespace GymManager.Application.Settings.Contracts;

public sealed record SettingResponse(Guid Id, string Key, string Value, string? Description, Guid? BranchId);

public static class SettingMappingExtensions
{
    public static SettingResponse ToResponse(this Setting setting) => new(setting.Id, setting.Key, setting.Value, setting.Description, setting.BranchId);
}
