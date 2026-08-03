using GymManager.SharedKernel.Cqrs;

namespace GymManager.Application.Staff.RescheduleShift;

public sealed record RescheduleShiftCommand(Guid ShiftId, DateTimeOffset StartUtc, DateTimeOffset EndUtc, string? Notes) : ICommand;
