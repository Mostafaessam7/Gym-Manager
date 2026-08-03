using FluentValidation;

namespace GymManager.Application.Settings.UpsertSetting;

public sealed class UpsertSettingCommandValidator : AbstractValidator<UpsertSettingCommand>
{
    public UpsertSettingCommandValidator()
    {
        RuleFor(c => c.Key).NotEmpty().MaximumLength(150);
        RuleFor(c => c.Value).NotNull();
    }
}
