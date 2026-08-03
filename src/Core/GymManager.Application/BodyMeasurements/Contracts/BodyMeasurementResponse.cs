namespace GymManager.Application.BodyMeasurements.Contracts;

public sealed record BodyMeasurementResponse(
    Guid Id,
    Guid MemberId,
    DateTimeOffset RecordedOnUtc,
    decimal? HeightCm,
    decimal? WeightKg,
    decimal? BodyFatPercentage,
    decimal? ChestCm,
    decimal? WaistCm,
    decimal? HipsCm,
    decimal? ArmCm,
    decimal? ThighCm,
    decimal? Bmi,
    string? Notes,
    string? PhotoUrl);
