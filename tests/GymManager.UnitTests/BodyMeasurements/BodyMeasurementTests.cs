using GymManager.Domain.BodyMeasurements;
using Xunit;

namespace GymManager.UnitTests.BodyMeasurements;

public sealed class BodyMeasurementTests
{
    [Fact]
    public void Record_Should_Capture_All_Given_Values()
    {
        var recordedOn = DateTimeOffset.UtcNow;
        var measurement = BodyMeasurement.Record(
            Guid.NewGuid(), recordedOn, heightCm: 180, weightKg: 80, bodyFatPercentage: 15,
            chestCm: 100, waistCm: 85, hipsCm: 95, armCm: 35, thighCm: 55, notes: "Feeling good");

        Assert.Equal(recordedOn, measurement.RecordedOnUtc);
        Assert.Equal(180, measurement.HeightCm);
        Assert.Equal(80, measurement.WeightKg);
        Assert.Equal("Feeling good", measurement.Notes);
    }

    [Fact]
    public void Bmi_Should_Be_Computed_From_Height_And_Weight()
    {
        var measurement = BodyMeasurement.Record(
            Guid.NewGuid(), DateTimeOffset.UtcNow, heightCm: 180, weightKg: 81, bodyFatPercentage: null,
            chestCm: null, waistCm: null, hipsCm: null, armCm: null, thighCm: null, notes: null);

        // 81 / (1.8 * 1.8) = 25.0
        Assert.Equal(25.0m, measurement.Bmi);
    }

    [Fact]
    public void Bmi_Should_Be_Null_When_Height_Is_Missing()
    {
        var measurement = BodyMeasurement.Record(
            Guid.NewGuid(), DateTimeOffset.UtcNow, heightCm: null, weightKg: 80, bodyFatPercentage: null,
            chestCm: null, waistCm: null, hipsCm: null, armCm: null, thighCm: null, notes: null);

        Assert.Null(measurement.Bmi);
    }

    [Fact]
    public void Bmi_Should_Be_Null_When_Weight_Is_Missing()
    {
        var measurement = BodyMeasurement.Record(
            Guid.NewGuid(), DateTimeOffset.UtcNow, heightCm: 180, weightKg: null, bodyFatPercentage: null,
            chestCm: null, waistCm: null, hipsCm: null, armCm: null, thighCm: null, notes: null);

        Assert.Null(measurement.Bmi);
    }

    [Fact]
    public void Update_Should_Replace_Every_Field()
    {
        var measurement = BodyMeasurement.Record(
            Guid.NewGuid(), DateTimeOffset.UtcNow, heightCm: 180, weightKg: 80, bodyFatPercentage: 15,
            chestCm: 100, waistCm: 85, hipsCm: 95, armCm: 35, thighCm: 55, notes: "Initial");

        var newRecordedOn = DateTimeOffset.UtcNow.AddDays(30);
        measurement.Update(newRecordedOn, 180, 78, 13, 99, 83, 94, 34, 54, "Progress!");

        Assert.Equal(newRecordedOn, measurement.RecordedOnUtc);
        Assert.Equal(78, measurement.WeightKg);
        Assert.Equal(13, measurement.BodyFatPercentage);
        Assert.Equal("Progress!", measurement.Notes);
    }

    [Fact]
    public void UpdatePhoto_Should_Set_The_Photo_Url()
    {
        var measurement = BodyMeasurement.Record(
            Guid.NewGuid(), DateTimeOffset.UtcNow, null, null, null, null, null, null, null, null, null);

        measurement.UpdatePhoto("/files/progress-photo.jpg");

        Assert.Equal("/files/progress-photo.jpg", measurement.PhotoUrl);
    }
}
