using Asp.Versioning;
using GymManager.Api.Authorization;
using GymManager.Api.Extensions;
using GymManager.Application.Notifications.GetNotifications;
using GymManager.Application.Notifications.SendNotification;
using GymManager.Domain.Identity;
using GymManager.Domain.Notifications;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Pagination;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymManager.Api.Controllers.V1;

/// <summary>Outbound email/SMS/in-app messages — both the one-off <c>SendNotification</c> command and the
/// record left behind by domain-event-driven notifications (welcome emails, receipts, reminders, alerts).</summary>
[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/notifications")]
public sealed class NotificationsController(IDispatcher dispatcher) : ControllerBase
{
    public sealed record SendNotificationRequest(
        NotificationChannel Channel, string RecipientAddress, string Subject, string Body, Guid? RecipientUserId, Guid? RecipientMemberId);

    [HttpGet]
    [HasPermission(Permissions.Notifications.Manage)]
    public async Task<IActionResult> GetNotifications(
        [FromQuery] PaginationParameters pagination, [FromQuery] Guid? recipientUserId, [FromQuery] Guid? recipientMemberId,
        [FromQuery] string? status, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(new GetNotificationsQuery(pagination, recipientUserId, recipientMemberId, status), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [HasPermission(Permissions.Notifications.Manage)]
    public async Task<IActionResult> SendNotification(SendNotificationRequest request, CancellationToken cancellationToken)
    {
        var command = new SendNotificationCommand(
            request.Channel, request.RecipientAddress, request.Subject, request.Body, request.RecipientUserId, request.RecipientMemberId);
        var result = await dispatcher.Send(command, cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblemDetails();
    }
}
