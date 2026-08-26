using GymManager.SharedKernel.Cqrs;

namespace GymManager.Application.Payments.HandleFawryWebhook;

/// <param name="Payload">The raw request body.</param>
/// <param name="Signature">The <c>signature</c> field FawryPay carries inside the notification's own JSON
/// body — the calling controller extracts it from the parsed payload since Fawry signs neither via a header
/// nor a query parameter.</param>
public sealed record HandleFawryWebhookCommand(string Payload, string Signature) : ICommand;
