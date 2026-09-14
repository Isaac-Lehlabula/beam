using Beam.Modules.Templates.Contracts;
using FluentValidation;

namespace Beam.Modules.Templates.Validators;

public class InstantiateTemplateRequestValidator : AbstractValidator<InstantiateTemplateRequest>
{
    public InstantiateTemplateRequestValidator()
    {
        RuleFor(x => x.ProjectName).NotEmpty().MaximumLength(120);
        RuleFor(x => x.ResolutionWidth).InclusiveBetween(160, 7680);
        RuleFor(x => x.ResolutionHeight).InclusiveBetween(90, 4320);
    }
}
