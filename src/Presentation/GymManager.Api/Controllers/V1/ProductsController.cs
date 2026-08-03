using Asp.Versioning;
using GymManager.Api.Authorization;
using GymManager.Api.Extensions;
using GymManager.Application.Products.CreateProduct;
using GymManager.Application.Products.DeactivateProduct;
using GymManager.Application.Products.GetProducts;
using GymManager.Application.Products.ReceiveStock;
using GymManager.Application.Products.UpdateProduct;
using GymManager.Domain.Identity;
using GymManager.Domain.Products;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Pagination;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymManager.Api.Controllers.V1;

/// <summary>Sellable inventory items — pricing, stock level, and stock-receiving. Sales themselves are
/// recorded through <c>SalesController</c>, which deducts from this stock.</summary>
[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/products")]
public sealed class ProductsController(IDispatcher dispatcher) : ControllerBase
{
    public sealed record ProductRequest(
        string Name, string Description, string Sku, ProductCategory Category, decimal Price, decimal CostPrice,
        string Currency, Guid BranchId, int InitialStock, int ReorderThreshold);

    public sealed record UpdateProductRequest(
        string Name, string Description, ProductCategory Category, decimal Price, decimal CostPrice, string Currency, int ReorderThreshold);

    public sealed record ReceiveStockRequest(int Quantity);

    [HttpGet]
    [HasPermission(Permissions.Products.View)]
    public async Task<IActionResult> GetProducts(
        [FromQuery] PaginationParameters pagination, [FromQuery] Guid? branchId, [FromQuery] bool? lowStockOnly,
        [FromQuery] bool includeInactive, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(new GetProductsQuery(pagination, branchId, lowStockOnly, includeInactive), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [HasPermission(Permissions.Products.Manage)]
    public async Task<IActionResult> CreateProduct(ProductRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateProductCommand(
            request.Name, request.Description, request.Sku, request.Category, request.Price, request.CostPrice,
            request.Currency, request.BranchId, request.InitialStock, request.ReorderThreshold);
        var result = await dispatcher.Send(command, cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPut("{id:guid}")]
    [HasPermission(Permissions.Products.Manage)]
    public async Task<IActionResult> UpdateProduct(Guid id, UpdateProductRequest request, CancellationToken cancellationToken)
    {
        var command = new UpdateProductCommand(
            id, request.Name, request.Description, request.Category, request.Price, request.CostPrice, request.Currency, request.ReorderThreshold);
        var result = await dispatcher.Send(command, cancellationToken);

        return result.IsSuccess ? NoContent() : result.ToProblemDetails();
    }

    [HttpPost("{id:guid}/receive-stock")]
    [HasPermission(Permissions.Inventory.Manage)]
    public async Task<IActionResult> ReceiveStock(Guid id, ReceiveStockRequest request, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(new ReceiveStockCommand(id, request.Quantity), cancellationToken);
        return result.IsSuccess ? NoContent() : result.ToProblemDetails();
    }

    [HttpPost("{id:guid}/deactivate")]
    [HasPermission(Permissions.Products.Manage)]
    public async Task<IActionResult> DeactivateProduct(Guid id, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(new DeactivateProductCommand(id), cancellationToken);
        return result.IsSuccess ? NoContent() : result.ToProblemDetails();
    }
}
