using GymManager.Application.Abstractions;
using GymManager.Application.Invoices.Contracts;
using GymManager.Domain.Invoices;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Pagination;
using Microsoft.EntityFrameworkCore;

namespace GymManager.Application.Invoices.GetInvoices;

public sealed class GetInvoicesQueryHandler(IApplicationReadDb readDb, IBranchAccessGuard branchAccessGuard)
    : IQueryHandler<GetInvoicesQuery, PagedList<InvoiceResponse>>
{
    public async Task<PagedList<InvoiceResponse>> Handle(GetInvoicesQuery query, CancellationToken cancellationToken)
    {
        var pagination = query.Pagination;
        var invoices = readDb.Invoices.AsQueryable();

        var branchId = branchAccessGuard.ResolveFilter(query.BranchId);
        if (branchId.HasValue)
            invoices = invoices.Where(i => i.BranchId == branchId);

        if (query.MemberId.HasValue)
            invoices = invoices.Where(i => i.MemberId == query.MemberId);

        if (!string.IsNullOrWhiteSpace(query.Status) && Enum.TryParse<InvoiceStatus>(query.Status, true, out var status))
            invoices = invoices.Where(i => i.Status == status);

        var totalCount = await invoices.CountAsync(cancellationToken);

        var items = await invoices
            .OrderByDescending(i => i.IssuedOnUtc)
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedList<InvoiceResponse>(items.Select(i => i.ToResponse()).ToList(), pagination.PageNumber, pagination.PageSize, totalCount);
    }
}
