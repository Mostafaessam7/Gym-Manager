using GymManager.SharedKernel.Results;

namespace GymManager.Domain.Lockers.Errors;

public static class LockerErrors
{
    public static readonly Error NotFound = Error.NotFound("Locker.NotFound", "The locker was not found.");

    public static Error NumberAlreadyInUse(string number) =>
        Error.Conflict("Locker.NumberAlreadyInUse", $"A locker numbered '{number}' already exists at this branch.");

    public static readonly Error NotAvailable = Error.Conflict("Locker.NotAvailable", "This locker is not currently available for assignment.");

    public static readonly Error NotAssigned = Error.Conflict("Locker.NotAssigned", "This locker is not currently assigned to anyone.");
}
