using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;

namespace GymManager.Application.Identity.Sessions.GetSessions;

public sealed record GetSessionsQuery(Guid UserId) : IQuery<Result<IReadOnlyCollection<SessionResponse>>>;
