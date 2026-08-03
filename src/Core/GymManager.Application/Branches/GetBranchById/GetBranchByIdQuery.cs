using GymManager.Application.Branches.Contracts;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;

namespace GymManager.Application.Branches.GetBranchById;

public sealed record GetBranchByIdQuery(Guid BranchId) : IQuery<Result<BranchResponse>>;
