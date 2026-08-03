using GymManager.Application.Payments.Contracts;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Pagination;

namespace GymManager.Application.Payments.GetPayments;

public sealed record GetPaymentsQuery(PaginationParameters Pagination, Guid? BranchId, Guid? MemberId, DateOnly? From, DateOnly? To)
    : IQuery<PagedList<PaymentResponse>>;
