using GymManager.Application.Abstractions;
using GymManager.Domain.Abstractions;
using GymManager.Domain.Settings;
using GymManager.Domain.Settings.Errors;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;

namespace GymManager.Application.Settings.DeleteSetting;

public sealed class DeleteSettingCommandHandler(
    ISettingRepository settingRepository, IUnitOfWork unitOfWork, IBranchAccessGuard branchAccessGuard)
    : ICommandHandler<DeleteSettingCommand>
{
    public async Task<Result> Handle(DeleteSettingCommand command, CancellationToken cancellationToken)
    {
        var setting = await settingRepository.GetByIdAsync(command.SettingId, cancellationToken);
        if (setting is null)
            return Result.Failure(SettingErrors.NotFound);

        // Guid.Empty is deliberate for a null BranchId (a global setting): a branch-scoped caller (whose
        // claim never equals Guid.Empty) is denied, matching how targeting any other branch that isn't
        // theirs is denied; an unscoped (HQ-level) caller is unaffected. Without this, a global setting
        // used to skip the check entirely and could be deleted by any branch-scoped caller.
        var accessResult = branchAccessGuard.EnsureCanAccess(setting.BranchId ?? Guid.Empty);
        if (accessResult.IsFailure)
            return accessResult;

        settingRepository.Remove(setting);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
