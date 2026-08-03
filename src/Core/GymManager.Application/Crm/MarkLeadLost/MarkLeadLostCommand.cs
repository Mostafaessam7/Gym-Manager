using GymManager.SharedKernel.Cqrs;

namespace GymManager.Application.Crm.MarkLeadLost;

public sealed record MarkLeadLostCommand(Guid LeadId, string? Reason) : ICommand;
