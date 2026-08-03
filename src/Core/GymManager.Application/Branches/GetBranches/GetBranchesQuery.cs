using GymManager.Application.Branches.Contracts;
using GymManager.SharedKernel.Cqrs;

namespace GymManager.Application.Branches.GetBranches;

public sealed record GetBranchesQuery(bool IncludeInactive) : IQuery<IReadOnlyList<BranchResponse>>;
