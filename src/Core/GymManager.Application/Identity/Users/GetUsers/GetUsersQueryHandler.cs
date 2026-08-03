using GymManager.Application.Abstractions;
using GymManager.Application.Identity.Contracts;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Pagination;
using Microsoft.EntityFrameworkCore;

namespace GymManager.Application.Identity.Users.GetUsers;

public sealed class GetUsersQueryHandler(IApplicationReadDb readDb, IBranchAccessGuard branchAccessGuard)
    : IQueryHandler<GetUsersQuery, PagedList<UserResponse>>
{
    public async Task<PagedList<UserResponse>> Handle(GetUsersQuery query, CancellationToken cancellationToken)
    {
        var pagination = query.Pagination;

        var usersQuery = readDb.Users.AsQueryable();

        var branchId = branchAccessGuard.ResolveFilter(null);
        if (branchId.HasValue)
            usersQuery = usersQuery.Where(u => u.BranchId == branchId);

        if (!string.IsNullOrWhiteSpace(pagination.SearchTerm))
        {
            var term = pagination.SearchTerm.Trim().ToLower();
            usersQuery = usersQuery.Where(u =>
                u.Email.Value.ToLower().Contains(term) ||
                u.FirstName.ToLower().Contains(term) ||
                u.LastName.ToLower().Contains(term));
        }

        usersQuery = pagination.SortBy?.ToLowerInvariant() switch
        {
            "email" => pagination.SortDescending ? usersQuery.OrderByDescending(u => u.Email.Value) : usersQuery.OrderBy(u => u.Email.Value),
            "firstname" => pagination.SortDescending ? usersQuery.OrderByDescending(u => u.FirstName) : usersQuery.OrderBy(u => u.FirstName),
            _ => usersQuery.OrderByDescending(u => u.CreatedOnUtc),
        };

        var totalCount = await usersQuery.CountAsync(cancellationToken);

        var users = await usersQuery
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync(cancellationToken);

        var roleIds = users.SelectMany(u => u.Roles.Select(r => r.RoleId)).Distinct().ToArray();
        var roles = await readDb.Roles.Where(r => roleIds.Contains(r.Id)).ToDictionaryAsync(r => r.Id, cancellationToken);

        var items = users.Select(u => new UserResponse(
                u.Id,
                u.Email.Value,
                u.FirstName,
                u.LastName,
                u.PhoneNumber,
                u.IsActive,
                u.BranchId,
                u.Roles.Where(r => roles.ContainsKey(r.RoleId)).Select(r => roles[r.RoleId].Name).ToArray(),
                u.CreatedOnUtc))
            .ToList();

        return new PagedList<UserResponse>(items, pagination.PageNumber, pagination.PageSize, totalCount);
    }
}
