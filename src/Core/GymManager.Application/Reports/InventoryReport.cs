using GymManager.Application.Abstractions;
using GymManager.SharedKernel.Cqrs;
using Microsoft.EntityFrameworkCore;

namespace GymManager.Application.Reports;

public sealed record InventoryReportRow(string ProductName, string Sku, int StockQuantity, int ReorderThreshold, bool IsLowStock, decimal StockValue, string Currency);

public sealed record InventoryReportQuery(Guid? BranchId) : IQuery<IReadOnlyList<InventoryReportRow>>;

public sealed class InventoryReportQueryHandler(IApplicationReadDb readDb) : IQueryHandler<InventoryReportQuery, IReadOnlyList<InventoryReportRow>>
{
    public async Task<IReadOnlyList<InventoryReportRow>> Handle(InventoryReportQuery query, CancellationToken cancellationToken)
    {
        var products = readDb.Products.AsQueryable();
        if (query.BranchId.HasValue)
            products = products.Where(p => p.BranchId == query.BranchId);

        return await products
            .OrderBy(p => p.Name)
            .Select(p => new InventoryReportRow(
                p.Name, p.Sku, p.StockQuantity, p.ReorderThreshold, p.StockQuantity <= p.ReorderThreshold,
                p.StockQuantity * p.CostPrice.Amount, p.CostPrice.Currency))
            .ToListAsync(cancellationToken);
    }
}
