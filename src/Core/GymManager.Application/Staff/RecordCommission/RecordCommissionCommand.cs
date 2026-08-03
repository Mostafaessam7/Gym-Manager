using GymManager.Application.Staff.Contracts;
using GymManager.Domain.Staff;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;

namespace GymManager.Application.Staff.RecordCommission;

public sealed record RecordCommissionCommand(
    Guid UserId, decimal Amount, CommissionSourceType SourceType, Guid? SourceReferenceId, DateTimeOffset EarnedOnUtc, string? Notes)
    : ICommand<Result<CommissionResponse>>;
