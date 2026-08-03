using GymManager.SharedKernel.Results;

namespace GymManager.Domain.Crm.Errors;

public static class LeadErrors
{
    public static readonly Error NotFound = Error.NotFound("Lead.NotFound", "The lead was not found.");

    public static readonly Error FollowUpNotFound = Error.NotFound("Lead.FollowUpNotFound", "The follow-up was not found on this lead.");

    public static readonly Error AlreadyConverted =
        Error.Conflict("Lead.AlreadyConverted", "This lead has already been converted to a member.");

    public static readonly Error NotWon = Error.Validation(
        "Lead.NotWon", "Use the mark-lost or convert endpoints to move a lead to a terminal stage, not this one.");
}
