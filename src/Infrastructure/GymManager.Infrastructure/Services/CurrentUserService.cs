using System.Security.Claims;
using GymManager.Application.Abstractions;
using Microsoft.AspNetCore.Http;

namespace GymManager.Infrastructure.Services;

/// <inheritdoc cref="ICurrentUserService"/>
public sealed class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    private ClaimsPrincipal? Principal => httpContextAccessor.HttpContext?.User;

    public Guid? UserId
    {
        get
        {
            var value = Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(value, out var id) ? id : null;
        }
    }

    public string? Email => Principal?.FindFirstValue(ClaimTypes.Email);

    public Guid? BranchId
    {
        get
        {
            var value = Principal?.FindFirstValue("branch_id");
            return Guid.TryParse(value, out var id) ? id : null;
        }
    }

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated ?? false;

    public IReadOnlyCollection<string> Permissions =>
        Principal?.FindAll("permission").Select(c => c.Value).ToArray() ?? [];

    public bool HasPermission(string permission) => Permissions.Contains(permission);
}
