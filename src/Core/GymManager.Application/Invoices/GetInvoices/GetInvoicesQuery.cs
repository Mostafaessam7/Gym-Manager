using GymManager.Application.Invoices.Contracts;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Pagination;

namespace GymManager.Application.Invoices.GetInvoices;

public sealed record GetInvoicesQuery(PaginationParameters Pagination, Guid? BranchId, Guid? MemberId, string? Status)
    : IQuery<PagedList<InvoiceResponse>>;
