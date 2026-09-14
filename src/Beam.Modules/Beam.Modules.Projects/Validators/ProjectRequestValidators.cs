using Beam.Modules.Projects.Contracts;
using FluentValidation;

namespace Beam.Modules.Projects.Validators;

public class CreateProjectRequestValidator : AbstractValidator<CreateProjectRequest>
{
    public CreateProjectRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
        RuleFor(x => x.ResolutionWidth).InclusiveBetween(160, 7680);
        RuleFor(x => x.ResolutionHeight).InclusiveBetween(90, 4320);
    }
}

public class UpdateProjectRequestValidator : AbstractValidator<UpdateProjectRequest>
{
    public UpdateProjectRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
    }
}
