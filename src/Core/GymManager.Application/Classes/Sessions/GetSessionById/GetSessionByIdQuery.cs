using GymManager.Application.Classes.Contracts;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;

namespace GymManager.Application.Classes.Sessions.GetSessionById;

public sealed record GetSessionByIdQuery(Guid SessionId) : IQuery<Result<ClassSessionResponse>>;
