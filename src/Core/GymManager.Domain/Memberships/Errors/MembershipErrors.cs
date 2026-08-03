using GymManager.SharedKernel.Results;

namespace GymManager.Domain.Memberships.Errors;

public static class MembershipPlanErrors
{
    public static readonly Error NotFound = Error.NotFound("MembershipPlan.NotFound", "The membership plan was not found.");

    public static Error NameAlreadyInUse(string name) =>
        Error.Conflict("MembershipPlan.NameAlreadyInUse", $"A membership plan named '{name}' already exists.");

    public static readonly Error Inactive = Error.Conflict("MembershipPlan.Inactive", "This membership plan is no longer available for purchase.");
}

public static class MembershipErrors
{
    public static readonly Error NotFound = Error.NotFound("Membership.NotFound", "The membership was not found.");

    public static readonly Error AlreadyCancelled = Error.Conflict("Membership.AlreadyCancelled", "This membership has already been cancelled.");

    public static readonly Error NotActive = Error.Conflict("Membership.NotActive", "Only an active membership can be frozen.");

    public static readonly Error NotFrozen = Error.Conflict("Membership.NotFrozen", "This membership is not currently frozen.");

    public static readonly Error CannotRenewCancelled = Error.Conflict("Membership.CannotRenewCancelled", "A cancelled membership cannot be renewed.");
}
