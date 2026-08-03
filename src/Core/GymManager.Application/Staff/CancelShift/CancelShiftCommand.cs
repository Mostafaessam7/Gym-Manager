using GymManager.SharedKernel.Cqrs;

namespace GymManager.Application.Staff.CancelShift;

public sealed record CancelShiftCommand(Guid ShiftId) : ICommand;
