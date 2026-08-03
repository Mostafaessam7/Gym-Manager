using GymManager.SharedKernel.Cqrs;

namespace GymManager.Application.Staff.MarkShiftNoShow;

public sealed record MarkShiftNoShowCommand(Guid ShiftId) : ICommand;
