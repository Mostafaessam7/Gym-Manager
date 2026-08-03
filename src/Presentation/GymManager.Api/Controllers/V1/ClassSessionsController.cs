using Asp.Versioning;
using GymManager.Api.Authorization;
using GymManager.Api.Extensions;
using GymManager.Application.Classes.Sessions.BookSession;
using GymManager.Application.Classes.Sessions.CancelBooking;
using GymManager.Application.Classes.Sessions.CancelSession;
using GymManager.Application.Classes.Sessions.GetSessionById;
using GymManager.Application.Classes.Sessions.GetSessions;
using GymManager.Application.Classes.Sessions.ScheduleSession;
using GymManager.Domain.Identity;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Pagination;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement;

namespace GymManager.Api.Controllers.V1;

/// <summary>Scheduled occurrences of a gym class — scheduling, cancellation, and member
/// booking/cancellation against a specific session's capacity.</summary>
[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/class-sessions")]
public sealed class ClassSessionsController(IDispatcher dispatcher, IFeatureManager featureManager) : ControllerBase
{
    private const string OnlineClassBookingFlag = "OnlineClassBooking";

    public sealed record ScheduleSessionRequest(Guid GymClassId, DateTimeOffset StartUtc, DateTimeOffset EndUtc, int? CapacityOverride);

    public sealed record BookSessionRequest(Guid MemberId);

    public sealed record CancelBookingRequest(Guid MemberId);

    [HttpGet]
    [HasPermission(Permissions.Classes.View)]
    public async Task<IActionResult> GetSessions(
        [FromQuery] PaginationParameters pagination, [FromQuery] Guid? branchId, [FromQuery] Guid? trainerId, [FromQuery] Guid? gymClassId,
        [FromQuery] DateTimeOffset? from, [FromQuery] DateTimeOffset? to, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(new GetSessionsQuery(pagination, branchId, trainerId, gymClassId, from, to), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [HasPermission(Permissions.Classes.View)]
    public async Task<IActionResult> GetSessionById(Guid id, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(new GetSessionByIdQuery(id), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPost]
    [HasPermission(Permissions.Classes.Manage)]
    public async Task<IActionResult> ScheduleSession(ScheduleSessionRequest request, CancellationToken cancellationToken)
    {
        var command = new ScheduleSessionCommand(request.GymClassId, request.StartUtc, request.EndUtc, request.CapacityOverride);
        var result = await dispatcher.Send(command, cancellationToken);

        return result.IsSuccess
            ? CreatedAtAction(nameof(GetSessionById), new { id = result.Value.Id }, result.Value)
            : result.ToProblemDetails();
    }

    [HttpPost("{id:guid}/cancel")]
    [HasPermission(Permissions.Classes.Manage)]
    public async Task<IActionResult> CancelSession(Guid id, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(new CancelSessionCommand(id), cancellationToken);
        return result.IsSuccess ? NoContent() : result.ToProblemDetails();
    }

    [HttpPost("{id:guid}/book")]
    [HasPermission(Permissions.Classes.Book)]
    public async Task<IActionResult> Book(Guid id, BookSessionRequest request, CancellationToken cancellationToken)
    {
        if (!await featureManager.IsEnabledAsync(OnlineClassBookingFlag))
        {
            return new ObjectResult(new ProblemDetails
            {
                Status = StatusCodes.Status503ServiceUnavailable,
                Title = "Feature.Disabled",
                Detail = "Online class booking is currently disabled.",
            })
            { StatusCode = StatusCodes.Status503ServiceUnavailable };
        }

        var result = await dispatcher.Send(new BookSessionCommand(id, request.MemberId), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPost("{id:guid}/cancel-booking")]
    [HasPermission(Permissions.Classes.Book)]
    public async Task<IActionResult> CancelBooking(Guid id, CancelBookingRequest request, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(new CancelBookingCommand(id, request.MemberId), cancellationToken);
        return result.IsSuccess ? NoContent() : result.ToProblemDetails();
    }
}
