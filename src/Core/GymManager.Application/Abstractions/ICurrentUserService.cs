namespace GymManager.Application.Abstractions;

/// <summary>Exposes the identity of the caller for the current request, resolved from the JWT principal.</summary>
public interface ICurrentUserService
{
    Guid? UserId { get; }

    string? Email { get; }

    Guid? BranchId { get; }

    bool IsAuthenticated { get; }

    IReadOnlyCollection<string> Permissions { get; }

    bool HasPermission(string permission);
}
