using GymManager.Application.Abstractions;
using GymManager.Application.Products.Contracts;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Pagination;
using Microsoft.EntityFrameworkCore;

namespace GymManager.Application.Products.GetProducts;

public sealed class GetProductsQueryHandler(IApplicationReadDb readDb, IBranchAccessGuard branchAccessGuard)
    : IQueryHandler<GetProductsQuery, PagedList<ProductResponse>>
{
    public async Task<PagedList<ProductResponse>> Handle(GetProductsQuery query, CancellationToken cancellationToken)
    {
        var pagination = query.Pagination;
        var products = readDb.Products.AsQueryable();

        var branchId = branchAccessGuard.ResolveFilter(query.BranchId);
        if (branchId.HasValue)
            products = products.Where(p => p.BranchId == branchId);

        if (!query.IncludeInactive)
            products = products.Where(p => p.IsActive);

        if (query.LowStockOnly == true)
            products = products.Where(p => p.StockQuantity <= p.ReorderThreshold);

        if (!string.IsNullOrWhiteSpace(pagination.SearchTerm))
        {
            var term = pagination.SearchTerm.Trim().ToLower();
            products = products.Where(p => p.Name.ToLower().Contains(term) || p.Sku.ToLower().Contains(term));
        }

        var totalCount = await products.CountAsync(cancellationToken);

        var items = await products
            .OrderBy(p => p.Name)
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedList<ProductResponse>(items.Select(p => p.ToResponse()).ToList(), pagination.PageNumber, pagination.PageSize, totalCount);
    }
}
