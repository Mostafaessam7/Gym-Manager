using GymManager.SharedKernel.Primitives;

namespace GymManager.Domain.BodyMeasurements;

/// <summary>A single point-in-time body-composition snapshot for a member — weight, body fat, and girth
/// measurements — used to chart progress over time. Height is captured on each record rather than looked up
/// from elsewhere, so BMI stays computable even if a later record's height differs (a growing teen member,
/// a correction of a previously mis-measured height, etc.).</summary>
public sealed class BodyMeasurement : AggregateRoot<Guid>
{
    private BodyMeasurement()
    {
    }

    private BodyMeasurement(
        Guid id,
        Guid memberId,
        DateTimeOffset recordedOnUtc,
        decimal? heightCm,
        decimal? weightKg,
        decimal? bodyFatPercentage,
        decimal? chestCm,
        decimal? waistCm,
        decimal? hipsCm,
        decimal? armCm,
        decimal? thighCm,
        string? notes,
        string? photoUrl)
        : base(id)
    {
        MemberId = memberId;
        RecordedOnUtc = recordedOnUtc;
        HeightCm = heightCm;
        WeightKg = weightKg;
        BodyFatPercentage = bodyFatPercentage;
        ChestCm = chestCm;
        WaistCm = waistCm;
        HipsCm = hipsCm;
        ArmCm = armCm;
        ThighCm = thighCm;
        Notes = notes;
        PhotoUrl = photoUrl;
    }

    public Guid MemberId { get; private set; }

    public DateTimeOffset RecordedOnUtc { get; private set; }

    public decimal? HeightCm { get; private set; }

    public decimal? WeightKg { get; private set; }

    public decimal? BodyFatPercentage { get; private set; }

    public decimal? ChestCm { get; private set; }

    public decimal? WaistCm { get; private set; }

    public decimal? HipsCm { get; private set; }

    public decimal? ArmCm { get; private set; }

    public decimal? ThighCm { get; private set; }

    public string? Notes { get; private set; }

    public string? PhotoUrl { get; private set; }

    /// <summary>Body Mass Index (kg/m²), or null when either height or weight is missing for this record.</summary>
    public decimal? Bmi
    {
        get
        {
            if (HeightCm is not { } heightCm || heightCm <= 0 || WeightKg is not { } weightKg)
                return null;

            var heightMeters = heightCm / 100m;
            return Math.Round(weightKg / (heightMeters * heightMeters), 2);
        }
    }

    public static BodyMeasurement Record(
        Guid memberId,
        DateTimeOffset recordedOnUtc,
        decimal? heightCm,
        decimal? weightKg,
        decimal? bodyFatPercentage,
        decimal? chestCm,
        decimal? waistCm,
        decimal? hipsCm,
        decimal? armCm,
        decimal? thighCm,
        string? notes) =>
        new(Guid.NewGuid(), memberId, recordedOnUtc, heightCm, weightKg, bodyFatPercentage,
            chestCm, waistCm, hipsCm, armCm, thighCm, notes?.Trim(), photoUrl: null);

    public void Update(
        DateTimeOffset recordedOnUtc,
        decimal? heightCm,
        decimal? weightKg,
        decimal? bodyFatPercentage,
        decimal? chestCm,
        decimal? waistCm,
        decimal? hipsCm,
        decimal? armCm,
        decimal? thighCm,
        string? notes)
    {
        RecordedOnUtc = recordedOnUtc;
        HeightCm = heightCm;
        WeightKg = weightKg;
        BodyFatPercentage = bodyFatPercentage;
        ChestCm = chestCm;
        WaistCm = waistCm;
        HipsCm = hipsCm;
        ArmCm = armCm;
        ThighCm = thighCm;
        Notes = notes?.Trim();
    }

    public void UpdatePhoto(string? photoUrl) => PhotoUrl = photoUrl;
}
