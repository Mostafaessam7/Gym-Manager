using GymManager.SharedKernel.Cqrs;

namespace GymManager.Application.Crm.CompleteLeadFollowUp;

public sealed record CompleteLeadFollowUpCommand(Guid LeadId, Guid FollowUpId, DateTimeOffset CompletedOnUtc, string? Notes) : ICommand;
