using GymManager.SharedKernel.Results;

namespace GymManager.Domain.Classes.Errors;

public static class GymClassErrors
{
    public static readonly Error NotFound = Error.NotFound("GymClass.NotFound", "The class was not found.");

    public static Error NameAlreadyInUse(string name) =>
        Error.Conflict("GymClass.NameAlreadyInUse", $"A class named '{name}' already exists.");
}

public static class ClassSessionErrors
{
    public static readonly Error NotFound = Error.NotFound("ClassSession.NotFound", "The class session was not found.");

    public static readonly Error NotScheduled = Error.Conflict("ClassSession.NotScheduled", "This session is not open for booking.");

    public static readonly Error SessionFull = Error.Conflict("ClassSession.SessionFull", "This session has reached its capacity.");

    public static readonly Error AlreadyBooked = Error.Conflict("ClassSession.AlreadyBooked", "This member already has an active booking for this session.");

    public static readonly Error BookingNotFound = Error.NotFound("ClassSession.BookingNotFound", "No active booking was found for this member.");

    public static readonly Error AlreadyCancelled = Error.Conflict("ClassSession.AlreadyCancelled", "This session has already been cancelled.");

    public static readonly Error EndBeforeStart = Error.Validation("ClassSession.EndBeforeStart", "The session end time must be after its start time.");

    public static readonly Error TrainerOverlap = Error.Conflict("ClassSession.TrainerOverlap", "This trainer already has a session scheduled during this time.");

    public static readonly Error MemberNotActive = Error.Forbidden("ClassSession.MemberNotActive", "This member's account is frozen or inactive.");

    public static readonly Error MembershipNotActive = Error.Forbidden("ClassSession.MembershipNotActive", "This member does not have an active membership.");
}
