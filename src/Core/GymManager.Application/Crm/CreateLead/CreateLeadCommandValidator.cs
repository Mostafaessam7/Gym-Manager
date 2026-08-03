using FluentValidation;

namespace GymManager.Application.Crm.CreateLead;

public sealed class CreateLeadCommandValidator : AbstractValidator<CreateLeadCommand>
{
    public CreateLeadCommandValidator()
    {
        RuleFor(c => c.Name).NotEmpty().MaximumLength(200);
        RuleFor(c => c.Email).EmailAddress().When(c => !string.IsNullOrWhiteSpace(c.Email));
        RuleFor(c => c.Phone).MaximumLength(30);
        RuleFor(c => c.Notes).MaximumLength(2000);
    }
}
