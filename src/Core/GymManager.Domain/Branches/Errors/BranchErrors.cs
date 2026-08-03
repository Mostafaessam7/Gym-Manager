using GymManager.SharedKernel.Results;

namespace GymManager.Domain.Branches.Errors;

public static class BranchErrors
{
    public static readonly Error NotFound = Error.NotFound("Branch.NotFound", "The branch was not found.");

    public static Error NameAlreadyInUse(string name) =>
        Error.Conflict("Branch.NameAlreadyInUse", $"A branch named '{name}' already exists.");

    public static readonly Error AlreadyInactive = Error.Conflict("Branch.AlreadyInactive", "The branch is already inactive.");

    public static readonly Error AccessDenied = Error.Forbidden(
        "Branch.AccessDenied", "You do not have access to this branch's data.");
}
