using Beam.Modules.AiGeneration.Contracts;
using FluentValidation;

namespace Beam.Modules.AiGeneration.Validators;

public class GenerateProjectRequestValidator : AbstractValidator<GenerateProjectRequest>
{
    public GenerateProjectRequestValidator()
    {
        RuleFor(x => x.Prompt).NotEmpty().MaximumLength(1000);
        RuleFor(x => x.ResolutionWidth).InclusiveBetween(160, 7680);
        RuleFor(x => x.ResolutionHeight).InclusiveBetween(90, 4320);
    }
}
