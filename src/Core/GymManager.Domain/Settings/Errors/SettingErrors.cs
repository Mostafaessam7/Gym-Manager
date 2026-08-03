using GymManager.SharedKernel.Results;

namespace GymManager.Domain.Settings.Errors;

public static class SettingErrors
{
    public static readonly Error NotFound = Error.NotFound("Setting.NotFound", "The setting was not found.");

    public static Error KeyAlreadyInUse(string key) =>
        Error.Conflict("Setting.KeyAlreadyInUse", $"A setting with key '{key}' already exists.");
}
