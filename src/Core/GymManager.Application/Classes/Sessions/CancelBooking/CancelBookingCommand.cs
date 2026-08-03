using GymManager.SharedKernel.Cqrs;

namespace GymManager.Application.Classes.Sessions.CancelBooking;

public sealed record CancelBookingCommand(Guid SessionId, Guid MemberId) : ICommand;
