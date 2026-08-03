using GymManager.SharedKernel.Cqrs;

namespace GymManager.Application.Classes.Sessions.CancelSession;

public sealed record CancelSessionCommand(Guid SessionId) : ICommand;
