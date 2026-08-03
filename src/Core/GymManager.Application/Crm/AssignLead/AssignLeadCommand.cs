using GymManager.SharedKernel.Cqrs;

namespace GymManager.Application.Crm.AssignLead;

public sealed record AssignLeadCommand(Guid LeadId, Guid? UserId) : ICommand;
