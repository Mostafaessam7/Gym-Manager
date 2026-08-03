using GymManager.Application.Settings.Contracts;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;

namespace GymManager.Application.Settings.UpsertSetting;

public sealed record UpsertSettingCommand(string Key, string Value, string? Description, Guid? BranchId) : ICommand<Result<SettingResponse>>;
