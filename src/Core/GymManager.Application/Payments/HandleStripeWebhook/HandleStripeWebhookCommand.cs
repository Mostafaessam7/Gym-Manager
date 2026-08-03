using GymManager.SharedKernel.Cqrs;

namespace GymManager.Application.Payments.HandleStripeWebhook;

public sealed record HandleStripeWebhookCommand(string Payload, string SignatureHeader) : ICommand;
