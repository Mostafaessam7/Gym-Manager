using GymManager.Domain.Trainers;
using Xunit;

namespace GymManager.UnitTests.Trainers;

public sealed class TrainerTests
{
    private static Trainer CreateTrainer() =>
        Trainer.Create(Guid.NewGuid(), "Alex", "Rivera", "Strength & Conditioning", null, null, null, null);

    [Fact]
    public void AddAvailabilitySlot_Should_Succeed_For_Non_Overlapping_Slots()
    {
        var trainer = CreateTrainer();

        var result = trainer.AddAvailabilitySlot(DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(11, 0));

        Assert.True(result.IsSuccess);
        Assert.Single(trainer.Availability);
    }

    [Fact]
    public void AddAvailabilitySlot_Should_Fail_When_Overlapping_An_Existing_Slot()
    {
        var trainer = CreateTrainer();
        trainer.AddAvailabilitySlot(DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(11, 0));

        var result = trainer.AddAvailabilitySlot(DayOfWeek.Monday, new TimeOnly(10, 0), new TimeOnly(12, 0));

        Assert.True(result.IsFailure);
        Assert.Equal("Trainer.SlotOverlaps", result.Error.Code);
    }

    [Fact]
    public void AddAvailabilitySlot_Should_Succeed_For_Same_Day_Non_Overlapping_Slot()
    {
        var trainer = CreateTrainer();
        trainer.AddAvailabilitySlot(DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(11, 0));

        var result = trainer.AddAvailabilitySlot(DayOfWeek.Monday, new TimeOnly(11, 0), new TimeOnly(13, 0));

        Assert.True(result.IsSuccess);
        Assert.Equal(2, trainer.Availability.Count);
    }

    [Fact]
    public void RemoveAvailabilitySlot_Should_Fail_When_Slot_Does_Not_Exist()
    {
        var trainer = CreateTrainer();

        var result = trainer.RemoveAvailabilitySlot(DayOfWeek.Friday, new TimeOnly(9, 0), new TimeOnly(11, 0));

        Assert.True(result.IsFailure);
        Assert.Equal("Trainer.SlotNotFound", result.Error.Code);
    }

    [Fact]
    public void IsAvailableAt_Should_Return_True_When_Requested_Window_Is_Within_A_Slot()
    {
        var trainer = CreateTrainer();
        trainer.AddAvailabilitySlot(DayOfWeek.Wednesday, new TimeOnly(8, 0), new TimeOnly(12, 0));

        Assert.True(trainer.IsAvailableAt(DayOfWeek.Wednesday, new TimeOnly(9, 0), new TimeOnly(10, 0)));
        Assert.False(trainer.IsAvailableAt(DayOfWeek.Wednesday, new TimeOnly(11, 0), new TimeOnly(13, 0)));
    }
}
