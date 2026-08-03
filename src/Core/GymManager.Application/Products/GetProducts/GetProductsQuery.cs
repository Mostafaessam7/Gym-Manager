using GymManager.Application.Products.Contracts;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Pagination;

namespace GymManager.Application.Products.GetProducts;

public sealed record GetProductsQuery(PaginationParameters Pagination, Guid? BranchId, bool? LowStockOnly, bool IncludeInactive)
    : IQuery<PagedList<ProductResponse>>;
