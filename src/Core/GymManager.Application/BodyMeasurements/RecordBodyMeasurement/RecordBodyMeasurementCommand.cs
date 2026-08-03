using GymManager.Application.BodyMeasurements.Contracts;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;

namespace GymManager.Application.BodyMeasurements.RecordBodyMeasurement;

public sealed record RecordBodyMeasurementCommand(
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
    string? Notes) : ICommand<Result<BodyMeasurementResponse>>;
