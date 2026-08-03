using GymManager.Domain.Abstractions;
using GymManager.Domain.Settings;
using GymManager.Domain.Settings.Errors;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;

namespace GymManager.Application.Settings.DeleteSetting;

public sealed class DeleteSettingCommandHandler(ISettingRepository settingRepository, IUnitOfWork unitOfWork)
    : ICommandHandler<DeleteSettingCommand>
{
    public async Task<Result> Handle(DeleteSettingCommand command, CancellationToken cancellationToken)
    {
        var setting = await settingRepository.GetByIdAsync(command.SettingId, cancellationToken);
        if (setting is null)
            return Result.Failure(SettingErrors.NotFound);

        settingRepository.Remove(setting);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
