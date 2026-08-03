using GymManager.SharedKernel.Cqrs;

namespace GymManager.Application.Attendance.CheckOut;

public sealed record CheckOutCommand(Guid MemberId) : ICommand;
