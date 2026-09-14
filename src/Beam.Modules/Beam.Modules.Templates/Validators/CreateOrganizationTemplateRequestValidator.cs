using Beam.Modules.Templates.Contracts;
using FluentValidation;

namespace Beam.Modules.Templates.Validators;

public class CreateOrganizationTemplateRequestValidator : AbstractValidator<CreateOrganizationTemplateRequest>
{
    public CreateOrganizationTemplateRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
        RuleFor(x => x.ProjectId).NotEmpty();
        RuleFor(x => x.OrganizationId).NotEmpty();
    }
}
