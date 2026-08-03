using GymManager.Application.Abstractions;
using GymManager.Application.Payments.Contracts;
using GymManager.Domain.Abstractions;
using GymManager.Domain.Common;
using GymManager.Domain.Payments;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;

namespace GymManager.Application.Payments.CreateGatewayPaymentIntent;

public sealed class CreateGatewayPaymentIntentCommandHandler(
    IPaymentRepository paymentRepository,
    IPaymentGatewayService paymentGatewayService,
    ICurrentUserService currentUserService,
    IUnitOfWork unitOfWork,
    IBranchAccessGuard branchAccessGuard)
    : ICommandHandler<CreateGatewayPaymentIntentCommand, Result<PaymentGatewayIntentResponse>>
{
    public async Task<Result<PaymentGatewayIntentResponse>> Handle(
        CreateGatewayPaymentIntentCommand command, CancellationToken cancellationToken)
    {
        var accessResult = branchAccessGuard.EnsureCanAccess(command.BranchId);
        if (accessResult.IsFailure)
            return Result.Failure<PaymentGatewayIntentResponse>(accessResult.Error);

        var amountResult = Money.Create(command.Amount, command.Currency);
        if (amountResult.IsFailure)
            return Result.Failure<PaymentGatewayIntentResponse>(amountResult.Error);

        var payment = Payment.Create(
            command.MemberId, command.BranchId, amountResult.Value, PaymentMethod.Card,
            command.ReferenceType, command.ReferenceId, currentUserService.UserId);

        var intentResult = await paymentGatewayService.CreatePaymentIntentAsync(
            amountResult.Value,
            command.ReceiptEmail,
            new Dictionary<string, string> { ["gymManagerPaymentId"] = payment.Id.ToString() },
            cancellationToken);

        if (intentResult.IsFailure)
            return Result.Failure<PaymentGatewayIntentResponse>(intentResult.Error);

        payment.AttachGatewayReference(PaymentGatewayProvider.Stripe, intentResult.Value.GatewayReferenceId);

        paymentRepository.Add(payment);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new PaymentGatewayIntentResponse(
            payment.Id, intentResult.Value.ClientSecret, paymentGatewayService.PublishableKey));
    }
}
