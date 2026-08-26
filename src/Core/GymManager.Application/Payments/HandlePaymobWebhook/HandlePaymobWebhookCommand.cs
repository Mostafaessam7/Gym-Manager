using GymManager.SharedKernel.Cqrs;

namespace GymManager.Application.Payments.HandlePaymobWebhook;

/// <param name="Payload">The raw request body.</param>
/// <param name="Hmac">The <c>hmac</c> query-string parameter Paymob attaches to the callback URL — carried
/// separately from the body since Paymob signs via a query parameter, not a header.</param>
public sealed record HandlePaymobWebhookCommand(string Payload, string Hmac) : ICommand;
