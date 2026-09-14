using Beam.Modules.Scenes.Contracts;
using FluentValidation;

namespace Beam.Modules.Scenes.Validators;

public class CreateSceneRequestValidator : AbstractValidator<CreateSceneRequest>
{
    public CreateSceneRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(80);
    }
}

public class RenameSceneRequestValidator : AbstractValidator<RenameSceneRequest>
{
    public RenameSceneRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(80);
    }
}

public class ReorderScenesRequestValidator : AbstractValidator<ReorderScenesRequest>
{
    public ReorderScenesRequestValidator()
    {
        RuleFor(x => x.OrderedSceneIds).NotEmpty();
    }
}
