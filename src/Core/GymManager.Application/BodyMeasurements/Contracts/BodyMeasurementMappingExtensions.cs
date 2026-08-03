using GymManager.Domain.BodyMeasurements;

namespace GymManager.Application.BodyMeasurements.Contracts;

public static class BodyMeasurementMappingExtensions
{
    public static BodyMeasurementResponse ToResponse(this BodyMeasurement measurement) => new(
        measurement.Id,
        measurement.MemberId,
        measurement.RecordedOnUtc,
        measurement.HeightCm,
        measurement.WeightKg,
        measurement.BodyFatPercentage,
        measurement.ChestCm,
        measurement.WaistCm,
        measurement.HipsCm,
        measurement.ArmCm,
        measurement.ThighCm,
        measurement.Bmi,
        measurement.Notes,
        measurement.PhotoUrl);
}
