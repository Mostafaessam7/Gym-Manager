using GymManager.Application.Abstractions;
using GymManager.Application.Payments.Contracts;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Pagination;
using Microsoft.EntityFrameworkCore;

namespace GymManager.Application.Payments.GetPayments;

public sealed class GetPaymentsQueryHandler(IApplicationReadDb readDb, IBranchAccessGuard branchAccessGuard)
    : IQueryHandler<GetPaymentsQuery, PagedList<PaymentResponse>>
{
    public async Task<PagedList<PaymentResponse>> Handle(GetPaymentsQuery query, CancellationToken cancellationToken)
    {
        var pagination = query.Pagination;
        var payments = readDb.Payments.AsQueryable();

        var branchId = branchAccessGuard.ResolveFilter(query.BranchId);
        if (branchId.HasValue)
            payments = payments.Where(p => p.BranchId == branchId);

        if (query.MemberId.HasValue)
            payments = payments.Where(p => p.MemberId == query.MemberId);

        if (query.From.HasValue)
            payments = payments.Where(p => p.CreatedOnUtc >= query.From.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc));

        if (query.To.HasValue)
            payments = payments.Where(p => p.CreatedOnUtc <= query.To.Value.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc));

        var totalCount = await payments.CountAsync(cancellationToken);

        var items = await payments
            .OrderByDescending(p => p.CreatedOnUtc)
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedList<PaymentResponse>(items.Select(p => p.ToResponse()).ToList(), pagination.PageNumber, pagination.PageSize, totalCount);
    }
}
