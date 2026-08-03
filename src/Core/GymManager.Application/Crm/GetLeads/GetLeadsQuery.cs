using GymManager.Application.Crm.Contracts;
using GymManager.Domain.Crm;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Pagination;

namespace GymManager.Application.Crm.GetLeads;

public sealed record GetLeadsQuery(
    PaginationParameters Pagination, LeadStage? Stage, Guid? AssignedToUserId, Guid? BranchId) : IQuery<PagedList<LeadResponse>>;
