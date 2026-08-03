using FluentValidation;

namespace GymManager.Application.Products.CreateProduct;

public sealed class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(c => c.Name).NotEmpty().MaximumLength(150);
        RuleFor(c => c.Sku).NotEmpty().MaximumLength(50);
        RuleFor(c => c.Price).GreaterThanOrEqualTo(0);
        RuleFor(c => c.CostPrice).GreaterThanOrEqualTo(0);
        RuleFor(c => c.Currency).NotEmpty().Length(3);
        RuleFor(c => c.BranchId).NotEmpty();
        RuleFor(c => c.InitialStock).GreaterThanOrEqualTo(0);
        RuleFor(c => c.ReorderThreshold).GreaterThanOrEqualTo(0);
    }
}
