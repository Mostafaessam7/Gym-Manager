using GymManager.SharedKernel.Cqrs;

namespace GymManager.Application.Settings.DeleteSetting;

public sealed record DeleteSettingCommand(Guid SettingId) : ICommand;
