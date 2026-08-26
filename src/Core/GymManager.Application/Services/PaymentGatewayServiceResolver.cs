using GymManager.Application.Abstractions;
using GymManager.Domain.Payments;
using GymManager.Domain.Payments.Errors;
using GymManager.SharedKernel.Results;

namespace GymManager.Application.Services;

/// <inheritdoc cref="IPaymentGatewayServiceResolver"/>
public sealed class PaymentGatewayServiceResolver(IEnumerable<IPaymentGatewayService> gateways) : IPaymentGatewayServiceResolver
{
    public Result<IPaymentGatewayService> Resolve(PaymentGatewayProvider provider)
    {
        var gateway = gateways.FirstOrDefault(g => g.Provider == provider);

        return gateway is null
            ? Result.Failure<IPaymentGatewayService>(PaymentErrors.GatewayNotConfigured(provider))
            : Result.Success(gateway);
    }
}
