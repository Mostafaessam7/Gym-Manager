using GymManager.Domain.Crm;
using GymManager.SharedKernel.Cqrs;

namespace GymManager.Application.Crm.UpdateLead;

public sealed record UpdateLeadCommand(Guid LeadId, string Name, string? Email, string? Phone, LeadSource Source, string? Notes) : ICommand;
