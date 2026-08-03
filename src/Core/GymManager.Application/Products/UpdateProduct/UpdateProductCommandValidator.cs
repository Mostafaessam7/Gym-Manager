using FluentValidation;

namespace GymManager.Application.Products.UpdateProduct;

public sealed class UpdateProductCommandValidator : AbstractValidator<UpdateProductCommand>
{
    public UpdateProductCommandValidator()
    {
        RuleFor(c => c.Name).NotEmpty().MaximumLength(150);
        RuleFor(c => c.Price).GreaterThanOrEqualTo(0);
        RuleFor(c => c.CostPrice).GreaterThanOrEqualTo(0);
        RuleFor(c => c.Currency).NotEmpty().Length(3);
        RuleFor(c => c.ReorderThreshold).GreaterThanOrEqualTo(0);
    }
}
