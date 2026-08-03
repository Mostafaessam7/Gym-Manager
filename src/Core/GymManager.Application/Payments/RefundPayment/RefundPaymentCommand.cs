using GymManager.SharedKernel.Cqrs;

namespace GymManager.Application.Payments.RefundPayment;

public sealed record RefundPaymentCommand(Guid PaymentId) : ICommand;
