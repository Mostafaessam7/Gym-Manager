using FluentValidation;

namespace GymManager.Application.BodyMeasurements.RecordBodyMeasurement;

public sealed class RecordBodyMeasurementCommandValidator : AbstractValidator<RecordBodyMeasurementCommand>
{
    public RecordBodyMeasurementCommandValidator()
    {
        RuleFor(c => c.MemberId).NotEmpty();
        RuleFor(c => c.HeightCm).GreaterThan(0).When(c => c.HeightCm.HasValue);
        RuleFor(c => c.WeightKg).GreaterThan(0).When(c => c.WeightKg.HasValue);
        RuleFor(c => c.BodyFatPercentage).InclusiveBetween(0, 100).When(c => c.BodyFatPercentage.HasValue);
        RuleFor(c => c.ChestCm).GreaterThan(0).When(c => c.ChestCm.HasValue);
        RuleFor(c => c.WaistCm).GreaterThan(0).When(c => c.WaistCm.HasValue);
        RuleFor(c => c.HipsCm).GreaterThan(0).When(c => c.HipsCm.HasValue);
        RuleFor(c => c.ArmCm).GreaterThan(0).When(c => c.ArmCm.HasValue);
        RuleFor(c => c.ThighCm).GreaterThan(0).When(c => c.ThighCm.HasValue);
        RuleFor(c => c.Notes).MaximumLength(2000);
    }
}
