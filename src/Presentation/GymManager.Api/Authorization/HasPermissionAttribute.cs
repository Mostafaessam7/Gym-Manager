using Microsoft.AspNetCore.Authorization;

namespace GymManager.Api.Authorization;

/// <summary>Restricts an endpoint to callers whose JWT carries the given permission code.</summary>
public sealed class HasPermissionAttribute(string permission) : AuthorizeAttribute($"permission:{permission}");
