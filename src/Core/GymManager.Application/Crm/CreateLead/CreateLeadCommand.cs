using GymManager.Application.Crm.Contracts;
using GymManager.Domain.Crm;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;

namespace GymManager.Application.Crm.CreateLead;

public sealed record CreateLeadCommand(
    string Name, string? Email, string? Phone, LeadSource Source, Guid? BranchId, Guid? AssignedToUserId, string? Notes)
    : ICommand<Result<LeadResponse>>;
