using GymManager.SharedKernel.Cqrs;

namespace GymManager.Application.Staff.CompleteShift;

public sealed record CompleteShiftCommand(Guid ShiftId) : ICommand;
