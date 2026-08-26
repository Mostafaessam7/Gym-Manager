namespace GymManager.Domain.Payments;

/// <summary>Which external payment processor (if any) collected a <see cref="Payment"/>. <see cref="None"/>
/// covers cash, manually-recorded bank transfers, and anything else settled outside a gateway.</summary>
public enum PaymentGatewayProvider
{
    None = 0,
    Stripe = 1,
    Paymob = 2,
    Fawry = 3,
}
