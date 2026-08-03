using GymManager.Application.Crm.Contracts;
using GymManager.Domain.Crm;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;

namespace GymManager.Application.Crm.AddLeadFollowUp;

public sealed record AddLeadFollowUpCommand(Guid LeadId, FollowUpType Type, DateTimeOffset ScheduledOnUtc, string? Notes)
    : ICommand<Result<LeadFollowUpResponse>>;
