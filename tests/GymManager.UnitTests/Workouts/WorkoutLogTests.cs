using GymManager.Domain.Workouts;
using Xunit;

namespace GymManager.UnitTests.Workouts;

public sealed class WorkoutLogTests
{
    [Fact]
    public void Record_Should_Capture_Member_Plan_And_Notes()
    {
        var memberId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        var completedOn = DateTimeOffset.UtcNow;

        var log = WorkoutLog.Record(memberId, planId, completedOn, durationMinutes: 45, "Felt strong");

        Assert.Equal(memberId, log.MemberId);
        Assert.Equal(planId, log.WorkoutPlanId);
        Assert.Equal(completedOn, log.CompletedOnUtc);
        Assert.Equal(45, log.DurationMinutes);
        Assert.Equal("Felt strong", log.Notes);
        Assert.Empty(log.Exercises);
    }

    [Fact]
    public void AddExercise_Should_Add_It_To_The_Log()
    {
        var log = WorkoutLog.Record(Guid.NewGuid(), null, DateTimeOffset.UtcNow, null, null);

        var exercise = log.AddExercise("Deadlift", setsCompleted: 3, repsCompleted: 5, weightKg: 100, notes: null);

        Assert.Single(log.Exercises);
        Assert.Equal(exercise.Id, log.Exercises.Single().Id);
        Assert.Equal("Deadlift", log.Exercises.Single().ExerciseName);
    }

    [Fact]
    public void Record_Without_A_Plan_Should_Leave_WorkoutPlanId_Null()
    {
        var log = WorkoutLog.Record(Guid.NewGuid(), null, DateTimeOffset.UtcNow, null, null);

        Assert.Null(log.WorkoutPlanId);
    }
}
