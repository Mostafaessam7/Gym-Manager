using GymManager.SharedKernel.Results;

namespace GymManager.Domain.Members.Errors;

public static class MemberErrors
{
    public static readonly Error NotFound = Error.NotFound("Member.NotFound", "The member was not found.");

    public static Error EmailAlreadyInUse(string email) =>
        Error.Conflict("Member.EmailAlreadyInUse", $"The email '{email}' is already registered to another member.");

    public static readonly Error AlreadyFrozen = Error.Conflict("Member.AlreadyFrozen", "The member is already frozen.");

    public static readonly Error NotFrozen = Error.Conflict("Member.NotFrozen", "The member is not currently frozen.");

    public static readonly Error CheckInCodeNotFound = Error.NotFound("Member.CheckInCodeNotFound", "No member matches this check-in code.");

    public static readonly Error DocumentNotFound = Error.NotFound("Member.DocumentNotFound", "The document was not found.");
}
