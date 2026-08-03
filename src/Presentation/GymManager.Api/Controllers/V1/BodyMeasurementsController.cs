using Asp.Versioning;
using GymManager.Api.Authorization;
using GymManager.Api.Extensions;
using GymManager.Application.BodyMeasurements.DeleteBodyMeasurement;
using GymManager.Application.BodyMeasurements.GetBodyMeasurements;
using GymManager.Application.BodyMeasurements.RecordBodyMeasurement;
using GymManager.Application.BodyMeasurements.UpdateBodyMeasurement;
using GymManager.Domain.Identity;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Pagination;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymManager.Api.Controllers.V1;

/// <summary>Body-composition snapshots (weight, body fat, girth measurements) recorded over time for a
/// member, used to chart progress. Reuses the <c>members:view</c>/<c>members:update</c> permissions since
/// this is member-scoped data, not a separately administered module.</summary>
[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/body-measurements")]
public sealed class BodyMeasurementsController(IDispatcher dispatcher) : ControllerBase
{
    public sealed record RecordMeasurementRequest(
        Guid MemberId,
        DateTimeOffset RecordedOnUtc,
        decimal? HeightCm,
        decimal? WeightKg,
        decimal? BodyFatPercentage,
        decimal? ChestCm,
        decimal? WaistCm,
        decimal? HipsCm,
        decimal? ArmCm,
        decimal? ThighCm,
        string? Notes);

    public sealed record UpdateMeasurementRequest(
        DateTimeOffset RecordedOnUtc,
        decimal? HeightCm,
        decimal? WeightKg,
        decimal? BodyFatPercentage,
        decimal? ChestCm,
        decimal? WaistCm,
        decimal? HipsCm,
        decimal? ArmCm,
        decimal? ThighCm,
        string? Notes);

    [HttpGet]
    [HasPermission(Permissions.Members.View)]
    public async Task<IActionResult> GetMeasurements(
        [FromQuery] Guid memberId, [FromQuery] PaginationParameters pagination, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(new GetBodyMeasurementsQuery(memberId, pagination), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [HasPermission(Permissions.Members.Update)]
    public async Task<IActionResult> RecordMeasurement(RecordMeasurementRequest request, CancellationToken cancellationToken)
    {
        var command = new RecordBodyMeasurementCommand(
            request.MemberId, request.RecordedOnUtc, request.HeightCm, request.WeightKg, request.BodyFatPercentage,
            request.ChestCm, request.WaistCm, request.HipsCm, request.ArmCm, request.ThighCm, request.Notes);

        var result = await dispatcher.Send(command, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPut("{id:guid}")]
    [HasPermission(Permissions.Members.Update)]
    public async Task<IActionResult> UpdateMeasurement(Guid id, UpdateMeasurementRequest request, CancellationToken cancellationToken)
    {
        var command = new UpdateBodyMeasurementCommand(
            id, request.RecordedOnUtc, request.HeightCm, request.WeightKg, request.BodyFatPercentage,
            request.ChestCm, request.WaistCm, request.HipsCm, request.ArmCm, request.ThighCm, request.Notes);

        var result = await dispatcher.Send(command, cancellationToken);
        return result.IsSuccess ? NoContent() : result.ToProblemDetails();
    }

    [HttpDelete("{id:guid}")]
    [HasPermission(Permissions.Members.Delete)]
    public async Task<IActionResult> DeleteMeasurement(Guid id, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(new DeleteBodyMeasurementCommand(id), cancellationToken);
        return result.IsSuccess ? NoContent() : result.ToProblemDetails();
    }
}
