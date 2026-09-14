using Beam.Modules.Elements.Contracts;
using FluentValidation;

namespace Beam.Modules.Elements.Validators;

public class CreateElementRequestValidator : AbstractValidator<CreateElementRequest>
{
    public CreateElementRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(80);
        RuleFor(x => x.Width).GreaterThan(0);
        RuleFor(x => x.Height).GreaterThan(0);
    }
}

public class UpdateElementTransformRequestValidator : AbstractValidator<UpdateElementTransformRequest>
{
    public UpdateElementTransformRequestValidator()
    {
        RuleFor(x => x.Width).GreaterThan(0);
        RuleFor(x => x.Height).GreaterThan(0);
        RuleFor(x => x.Opacity).InclusiveBetween(0, 1);
    }
}
