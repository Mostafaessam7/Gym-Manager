using GymManager.Domain.Workouts;
using Xunit;

namespace GymManager.UnitTests.Workouts;

public sealed class WorkoutPlanTests
{
    private static WorkoutPlan CreatePlan() =>
        WorkoutPlan.Create(Guid.NewGuid(), Guid.NewGuid(), "Push/Pull/Legs", "A 3-day split");

    [Fact]
    public void Create_Should_Set_Name_And_Default_To_Active()
    {
        var plan = CreatePlan();

        Assert.Equal("Push/Pull/Legs", plan.Name);
        Assert.True(plan.IsActive);
        Assert.Empty(plan.Exercises);
    }

    [Fact]
    public void AddExercise_Should_Add_It_To_The_Plan()
    {
        var plan = CreatePlan();

        var exercise = plan.AddExercise("Bench Press", dayNumber: 1, order: 1, sets: 4, reps: 8, weightKg: 60, null, restSeconds: 90, notes: null);

        Assert.Single(plan.Exercises);
        Assert.Equal(exercise.Id, plan.Exercises.Single().Id);
        Assert.Equal("Bench Press", plan.Exercises.Single().ExerciseName);
    }

    [Fact]
    public void UpdateExercise_Should_Fail_For_An_Unknown_Id()
    {
        var plan = CreatePlan();

        var result = plan.UpdateExercise(Guid.NewGuid(), "Squat", 1, 1, 4, 8, null, null, null, null);

        Assert.True(result.IsFailure);
        Assert.Equal("Workout.ExerciseNotFound", result.Error.Code);
    }

    [Fact]
    public void UpdateExercise_Should_Replace_The_Fields_Of_The_Matching_Exercise()
    {
        var plan = CreatePlan();
        var exercise = plan.AddExercise("Bench Press", 1, 1, 4, 8, 60, null, 90, null);

        var result = plan.UpdateExercise(exercise.Id, "Incline Bench Press", 1, 1, 3, 10, 55, null, 60, "Lighter, more reps");

        Assert.True(result.IsSuccess);
        var updated = plan.Exercises.Single();
        Assert.Equal("Incline Bench Press", updated.ExerciseName);
        Assert.Equal(3, updated.Sets);
        Assert.Equal("Lighter, more reps", updated.Notes);
    }

    [Fact]
    public void RemoveExercise_Should_Remove_The_Matching_Exercise()
    {
        var plan = CreatePlan();
        var exercise = plan.AddExercise("Bench Press", 1, 1, 4, 8, 60, null, 90, null);

        var result = plan.RemoveExercise(exercise.Id);

        Assert.True(result.IsSuccess);
        Assert.Empty(plan.Exercises);
    }

    [Fact]
    public void RemoveExercise_Should_Fail_For_An_Unknown_Id()
    {
        var plan = CreatePlan();

        var result = plan.RemoveExercise(Guid.NewGuid());

        Assert.True(result.IsFailure);
        Assert.Equal("Workout.ExerciseNotFound", result.Error.Code);
    }

    [Fact]
    public void Deactivate_Then_Activate_Should_Toggle_IsActive()
    {
        var plan = CreatePlan();

        plan.Deactivate();
        Assert.False(plan.IsActive);

        plan.Activate();
        Assert.True(plan.IsActive);
    }
}
