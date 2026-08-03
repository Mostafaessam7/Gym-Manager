using GymManager.Application.Abstractions;
using GymManager.Application.Settings.Contracts;
using GymManager.Domain.Abstractions;
using GymManager.Domain.Settings;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;

namespace GymManager.Application.Settings.UpsertSetting;

public sealed class UpsertSettingCommandHandler(
    ISettingRepository settingRepository, IUnitOfWork unitOfWork, IBranchAccessGuard branchAccessGuard)
    : ICommandHandler<UpsertSettingCommand, Result<SettingResponse>>
{
    public async Task<Result<SettingResponse>> Handle(UpsertSettingCommand command, CancellationToken cancellationToken)
    {
        if (command.BranchId.HasValue)
        {
            var accessResult = branchAccessGuard.EnsureCanAccess(command.BranchId.Value);
            if (accessResult.IsFailure)
                return Result.Failure<SettingResponse>(accessResult.Error);
        }

        var existing = await settingRepository.GetByKeyAsync(command.Key.Trim(), command.BranchId, cancellationToken);

        if (existing is not null)
        {
            existing.UpdateValue(command.Value, command.Description);
            settingRepository.Update(existing);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success(existing.ToResponse());
        }

        var setting = Setting.Create(command.Key, command.Value, command.Description, command.BranchId);
        settingRepository.Add(setting);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(setting.ToResponse());
    }
}
