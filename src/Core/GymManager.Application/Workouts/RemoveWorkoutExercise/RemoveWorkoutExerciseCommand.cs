using GymManager.SharedKernel.Cqrs;

namespace GymManager.Application.Workouts.RemoveWorkoutExercise;

public sealed record RemoveWorkoutExerciseCommand(Guid PlanId, Guid ExerciseId) : ICommand;
