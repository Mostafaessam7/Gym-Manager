using GymManager.Application.Abstractions;
using GymManager.Application.Branches.Contracts;
using GymManager.SharedKernel.Cqrs;
using Microsoft.EntityFrameworkCore;

namespace GymManager.Application.Branches.GetBranches;

public sealed class GetBranchesQueryHandler(IApplicationReadDb readDb, ICacheService cacheService)
    : IQueryHandler<GetBranchesQuery, IReadOnlyList<BranchResponse>>
{
    public const string CacheKeyPrefix = "branches:list:";

    public static void InvalidateCache(ICacheService cacheService)
    {
        cacheService.Remove($"{CacheKeyPrefix}True");
        cacheService.Remove($"{CacheKeyPrefix}False");
    }

    public Task<IReadOnlyList<BranchResponse>> Handle(GetBranchesQuery query, CancellationToken cancellationToken) =>
        cacheService.GetOrCreateAsync(
            $"{CacheKeyPrefix}{query.IncludeInactive}",
            async ct =>
            {
                var branches = readDb.Branches.AsQueryable();

                if (!query.IncludeInactive)
                    branches = branches.Where(b => b.IsActive);

                var results = await branches.OrderBy(b => b.Name).ToListAsync(ct);

                return (IReadOnlyList<BranchResponse>)results.Select(b => new BranchResponse(
                        b.Id, b.Name, b.Address.Street, b.Address.City, b.Address.State,
                        b.Address.PostalCode, b.Address.Country, b.PhoneNumber, b.Email, b.IsActive))
                    .ToList();
            },
            TimeSpan.FromMinutes(10),
            cancellationToken);
}
