using GymManager.Application.Sales.Contracts;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Pagination;

namespace GymManager.Application.Sales.GetSales;

public sealed record GetSalesQuery(PaginationParameters Pagination, Guid? BranchId, Guid? MemberId) : IQuery<PagedList<SaleResponse>>;
