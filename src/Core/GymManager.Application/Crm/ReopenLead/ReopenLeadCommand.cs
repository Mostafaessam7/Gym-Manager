using GymManager.SharedKernel.Cqrs;

namespace GymManager.Application.Crm.ReopenLead;

public sealed record ReopenLeadCommand(Guid LeadId) : ICommand;
