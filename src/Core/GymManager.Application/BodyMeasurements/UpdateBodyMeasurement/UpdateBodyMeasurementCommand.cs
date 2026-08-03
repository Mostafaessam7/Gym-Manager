using GymManager.SharedKernel.Cqrs;

namespace GymManager.Application.BodyMeasurements.UpdateBodyMeasurement;

public sealed record UpdateBodyMeasurementCommand(
    Guid MeasurementId,
    DateTimeOffset RecordedOnUtc,
    decimal? HeightCm,
    decimal? WeightKg,
    decimal? BodyFatPercentage,
    decimal? ChestCm,
    decimal? WaistCm,
    decimal? HipsCm,
    decimal? ArmCm,
    decimal? ThighCm,
    string? Notes) : ICommand;
