using Asp.Versioning;
using GymManager.Api.Authorization;
using GymManager.Api.Extensions;
using GymManager.Application.Nutrition.AddNutritionMeal;
using GymManager.Application.Nutrition.Contracts;
using GymManager.Application.Nutrition.CreateNutritionPlan;
using GymManager.Application.Nutrition.DeleteNutritionPlan;
using GymManager.Application.Nutrition.GetNutritionPlanById;
using GymManager.Application.Nutrition.GetNutritionPlans;
using GymManager.Application.Nutrition.RemoveNutritionMeal;
using GymManager.Application.Nutrition.UpdateNutritionMeal;
using GymManager.Application.Nutrition.UpdateNutritionPlan;
using GymManager.Domain.Identity;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Pagination;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymManager.Api.Controllers.V1;

/// <summary>Trainer/dietitian-assigned nutrition plans — daily macro targets plus a named set of meals. See
/// <see cref="NutritionLogsController"/> for what members actually ate.</summary>
[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/nutrition-plans")]
public sealed class NutritionController(IDispatcher dispatcher) : ControllerBase
{
    public sealed record CreatePlanRequest(
        Guid MemberId,
        Guid? TrainerId,
        string Name,
        string? Description,
        int? DailyCalorieTarget,
        decimal? ProteinTargetG,
        decimal? CarbsTargetG,
        decimal? FatTargetG,
        IReadOnlyCollection<NutritionMealInput> Meals);

    public sealed record UpdatePlanRequest(
        string Name,
        string? Description,
        int? DailyCalorieTarget,
        decimal? ProteinTargetG,
        decimal? CarbsTargetG,
        decimal? FatTargetG,
        bool IsActive);

    public sealed record AddMealRequest(NutritionMealInput Meal);

    public sealed record UpdateMealRequest(NutritionMealInput Meal);

    [HttpGet]
    [HasPermission(Permissions.Nutrition.View)]
    public async Task<IActionResult> GetPlans(
        [FromQuery] Guid memberId, [FromQuery] PaginationParameters pagination, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(new GetNutritionPlansQuery(memberId, pagination), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [HasPermission(Permissions.Nutrition.View)]
    public async Task<IActionResult> GetPlanById(Guid id, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(new GetNutritionPlanByIdQuery(id), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPost]
    [HasPermission(Permissions.Nutrition.Manage)]
    public async Task<IActionResult> CreatePlan(CreatePlanRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateNutritionPlanCommand(
            request.MemberId, request.TrainerId, request.Name, request.Description, request.DailyCalorieTarget,
            request.ProteinTargetG, request.CarbsTargetG, request.FatTargetG, request.Meals);
        var result = await dispatcher.Send(command, cancellationToken);

        return result.IsSuccess
            ? CreatedAtAction(nameof(GetPlanById), new { id = result.Value.Id }, result.Value)
            : result.ToProblemDetails();
    }

    [HttpPut("{id:guid}")]
    [HasPermission(Permissions.Nutrition.Manage)]
    public async Task<IActionResult> UpdatePlan(Guid id, UpdatePlanRequest request, CancellationToken cancellationToken)
    {
        var command = new UpdateNutritionPlanCommand(
            id, request.Name, request.Description, request.DailyCalorieTarget,
            request.ProteinTargetG, request.CarbsTargetG, request.FatTargetG, request.IsActive);
        var result = await dispatcher.Send(command, cancellationToken);

        return result.IsSuccess ? NoContent() : result.ToProblemDetails();
    }

    [HttpDelete("{id:guid}")]
    [HasPermission(Permissions.Nutrition.Manage)]
    public async Task<IActionResult> DeletePlan(Guid id, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(new DeleteNutritionPlanCommand(id), cancellationToken);
        return result.IsSuccess ? NoContent() : result.ToProblemDetails();
    }

    [HttpPost("{id:guid}/meals")]
    [HasPermission(Permissions.Nutrition.Manage)]
    public async Task<IActionResult> AddMeal(Guid id, AddMealRequest request, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(new AddNutritionMealCommand(id, request.Meal), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPut("{id:guid}/meals/{mealId:guid}")]
    [HasPermission(Permissions.Nutrition.Manage)]
    public async Task<IActionResult> UpdateMeal(Guid id, Guid mealId, UpdateMealRequest request, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(new UpdateNutritionMealCommand(id, mealId, request.Meal), cancellationToken);
        return result.IsSuccess ? NoContent() : result.ToProblemDetails();
    }

    [HttpDelete("{id:guid}/meals/{mealId:guid}")]
    [HasPermission(Permissions.Nutrition.Manage)]
    public async Task<IActionResult> RemoveMeal(Guid id, Guid mealId, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(new RemoveNutritionMealCommand(id, mealId), cancellationToken);
        return result.IsSuccess ? NoContent() : result.ToProblemDetails();
    }
}
