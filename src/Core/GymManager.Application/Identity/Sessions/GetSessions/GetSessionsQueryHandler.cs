using GymManager.Application.Abstractions;
using GymManager.Domain.Identity.Errors;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace GymManager.Application.Identity.Sessions.GetSessions;

/// <summary>Lists every refresh-token "session" for the caller, active and expired/revoked alike, so a
/// "manage my devices" screen can show history as well as what's currently signed in.</summary>
public sealed class GetSessionsQueryHandler(IApplicationReadDb readDb)
    : IQueryHandler<GetSessionsQuery, Result<IReadOnlyCollection<SessionResponse>>>
{
    public async Task<Result<IReadOnlyCollection<SessionResponse>>> Handle(GetSessionsQuery query, CancellationToken cancellationToken)
    {
        var user = await readDb.Users.FirstOrDefaultAsync(u => u.Id == query.UserId, cancellationToken);
        if (user is null)
            return Result.Failure<IReadOnlyCollection<SessionResponse>>(UserErrors.NotFound);

        var sessions = user.RefreshTokens
            .OrderByDescending(t => t.CreatedOnUtc)
            .Select(t => new SessionResponse(t.Id, t.IpAddress, t.UserAgent, t.CreatedOnUtc, t.ExpiresOnUtc, t.IsActive))
            .ToArray();

        return Result.Success<IReadOnlyCollection<SessionResponse>>(sessions);
    }
}
