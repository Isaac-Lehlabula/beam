using Beam.Modules.Elements.Contracts;
using FluentValidation;

namespace Beam.Modules.Elements.Validators;

public class TextElementConfigValidator : AbstractValidator<TextElementConfig>
{
    private static readonly string[] AllowedAlignments = ["left", "center", "right"];

    public TextElementConfigValidator()
    {
        RuleFor(x => x.Content).NotNull().MaximumLength(2000);
        RuleFor(x => x.FontFamily).NotEmpty().MaximumLength(80);
        RuleFor(x => x.FontSize).InclusiveBetween(1, 400);
        RuleFor(x => x.FontWeight).NotEmpty().MaximumLength(20);
        RuleFor(x => x.TextAlign).Must(v => AllowedAlignments.Contains(v))
            .WithMessage($"TextAlign must be one of: {string.Join(", ", AllowedAlignments)}");
        RuleFor(x => x.Color).NotEmpty().MaximumLength(30);
    }
}

public class ImageElementConfigValidator : AbstractValidator<ImageElementConfig>
{
    private static readonly string[] AllowedFits = ["cover", "contain", "fill"];

    public ImageElementConfigValidator()
    {
        RuleFor(x => x.AssetId).NotEmpty();
        RuleFor(x => x.Fit).Must(v => AllowedFits.Contains(v))
            .WithMessage($"Fit must be one of: {string.Join(", ", AllowedFits)}");
    }
}
