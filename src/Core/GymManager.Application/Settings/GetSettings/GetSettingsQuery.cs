using GymManager.Application.Settings.Contracts;
using GymManager.SharedKernel.Cqrs;

namespace GymManager.Application.Settings.GetSettings;

public sealed record GetSettingsQuery(Guid? BranchId) : IQuery<IReadOnlyList<SettingResponse>>;
