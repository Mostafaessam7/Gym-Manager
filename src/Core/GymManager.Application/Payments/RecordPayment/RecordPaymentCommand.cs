using GymManager.Application.Payments.Contracts;
using GymManager.Domain.Payments;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;

namespace GymManager.Application.Payments.RecordPayment;

public sealed record RecordPaymentCommand(
    Guid MemberId,
    Guid BranchId,
    decimal Amount,
    string Currency,
    PaymentMethod Method,
    PaymentReferenceType ReferenceType,
    Guid? ReferenceId) : ICommand<Result<PaymentResponse>>;
