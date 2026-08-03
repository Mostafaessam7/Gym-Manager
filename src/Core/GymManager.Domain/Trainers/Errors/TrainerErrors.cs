using GymManager.SharedKernel.Results;

namespace GymManager.Domain.Trainers.Errors;

public static class TrainerErrors
{
    public static readonly Error NotFound = Error.NotFound("Trainer.NotFound", "The trainer was not found.");

    public static Error EmailAlreadyInUse(string email) =>
        Error.Conflict("Trainer.EmailAlreadyInUse", $"The email '{email}' is already registered to another trainer.");

    public static readonly Error SlotOverlaps = Error.Conflict("Trainer.SlotOverlaps", "This availability slot overlaps with an existing one.");

    public static readonly Error SlotNotFound = Error.NotFound("Trainer.SlotNotFound", "The availability slot was not found.");
}
