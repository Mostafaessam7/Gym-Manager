using GymManager.Application.Crm.Contracts;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;

namespace GymManager.Application.Crm.GetLeadById;

public sealed record GetLeadByIdQuery(Guid LeadId) : IQuery<Result<LeadResponse>>;
