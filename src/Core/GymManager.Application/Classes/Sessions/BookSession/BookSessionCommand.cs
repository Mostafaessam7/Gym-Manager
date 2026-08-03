using GymManager.Application.Classes.Contracts;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;

namespace GymManager.Application.Classes.Sessions.BookSession;

public sealed record BookSessionCommand(Guid SessionId, Guid MemberId) : ICommand<Result<ClassSessionResponse>>;
