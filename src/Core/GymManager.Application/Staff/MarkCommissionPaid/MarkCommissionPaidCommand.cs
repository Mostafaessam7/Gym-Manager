using GymManager.SharedKernel.Cqrs;

namespace GymManager.Application.Staff.MarkCommissionPaid;

public sealed record MarkCommissionPaidCommand(Guid CommissionId) : ICommand;
