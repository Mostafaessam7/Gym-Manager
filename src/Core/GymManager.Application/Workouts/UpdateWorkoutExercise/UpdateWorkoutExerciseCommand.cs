using GymManager.Application.Workouts.Contracts;
using GymManager.SharedKernel.Cqrs;

namespace GymManager.Application.Workouts.UpdateWorkoutExercise;

public sealed record UpdateWorkoutExerciseCommand(Guid PlanId, Guid ExerciseId, WorkoutExerciseInput Exercise) : ICommand;
