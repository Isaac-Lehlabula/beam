using Beam.Modules.Elements.Contracts;
using FluentValidation;

namespace Beam.Modules.Elements.Validators;

public class VideoElementConfigValidator : AbstractValidator<VideoElementConfig>
{
    private static readonly string[] AllowedFits = ["cover", "contain", "fill"];

    public VideoElementConfigValidator()
    {
        RuleFor(x => x.AssetId).NotEmpty();
        RuleFor(x => x.Fit).Must(v => AllowedFits.Contains(v)).WithMessage($"Fit must be one of: {string.Join(", ", AllowedFits)}");
    }
}

public class CameraElementConfigValidator : AbstractValidator<CameraElementConfig>
{
    public CameraElementConfigValidator()
    {
        RuleFor(x => x.Label).NotNull().MaximumLength(80);
        RuleFor(x => x.BorderColor).NotEmpty().MaximumLength(30);
    }
}

public class StatCounterElementConfigValidator : AbstractValidator<StatCounterElementConfig>
{
    private static readonly string[] AllowedKinds = ["viewers", "subscribers", "followers"];

    public StatCounterElementConfigValidator()
    {
        RuleFor(x => x.Kind).Must(v => AllowedKinds.Contains(v)).WithMessage($"Kind must be one of: {string.Join(", ", AllowedKinds)}");
        RuleFor(x => x.Label).NotNull().MaximumLength(60);
        RuleFor(x => x.ManualValue).GreaterThanOrEqualTo(0);
        RuleFor(x => x.FontFamily).NotEmpty().MaximumLength(80);
        RuleFor(x => x.FontSize).InclusiveBetween(1, 400);
        RuleFor(x => x.Color).NotEmpty().MaximumLength(30);
    }
}

public class QrCodeElementConfigValidator : AbstractValidator<QrCodeElementConfig>
{
    public QrCodeElementConfigValidator()
    {
        RuleFor(x => x.Value).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.ForegroundColor).NotEmpty().MaximumLength(30);
        RuleFor(x => x.BackgroundColor).NotEmpty().MaximumLength(30);
    }
}

public class CountdownElementConfigValidator : AbstractValidator<CountdownElementConfig>
{
    public CountdownElementConfigValidator()
    {
        RuleFor(x => x.Label).NotNull().MaximumLength(80);
        RuleFor(x => x.CompletedText).NotNull().MaximumLength(80);
        RuleFor(x => x.FontFamily).NotEmpty().MaximumLength(80);
        RuleFor(x => x.FontSize).InclusiveBetween(1, 400);
        RuleFor(x => x.Color).NotEmpty().MaximumLength(30);
    }
}

public class ProgressBarElementConfigValidator : AbstractValidator<ProgressBarElementConfig>
{
    public ProgressBarElementConfigValidator()
    {
        RuleFor(x => x.Label).NotNull().MaximumLength(80);
        RuleFor(x => x.TargetValue).GreaterThan(0);
        RuleFor(x => x.CurrentValue).GreaterThanOrEqualTo(0);
        RuleFor(x => x.BarColor).NotEmpty().MaximumLength(30);
        RuleFor(x => x.TrackColor).NotEmpty().MaximumLength(30);
        RuleFor(x => x.TextColor).NotEmpty().MaximumLength(30);
    }
}

public class TickerElementConfigValidator : AbstractValidator<TickerElementConfig>
{
    public TickerElementConfigValidator()
    {
        RuleFor(x => x.Items).NotEmpty();
        RuleForEach(x => x.Items).NotEmpty().MaximumLength(300);
        RuleFor(x => x.SpeedPxPerSecond).InclusiveBetween(1, 2000);
        RuleFor(x => x.BackgroundColor).NotEmpty().MaximumLength(30);
        RuleFor(x => x.TextColor).NotEmpty().MaximumLength(30);
        RuleFor(x => x.FontSize).InclusiveBetween(1, 400);
    }
}

public class LowerThirdElementConfigValidator : AbstractValidator<LowerThirdElementConfig>
{
    public LowerThirdElementConfigValidator()
    {
        RuleFor(x => x.PrimaryText).NotNull().MaximumLength(120);
        RuleFor(x => x.SecondaryText).NotNull().MaximumLength(120);
        RuleFor(x => x.AccentColor).NotEmpty().MaximumLength(30);
        RuleFor(x => x.TextColor).NotEmpty().MaximumLength(30);
        RuleFor(x => x.BackgroundColor).NotEmpty().MaximumLength(30);
    }
}

public class ImageCarouselElementConfigValidator : AbstractValidator<ImageCarouselElementConfig>
{
    private static readonly string[] AllowedFits = ["cover", "contain", "fill"];

    public ImageCarouselElementConfigValidator()
    {
        RuleFor(x => x.AssetIds).NotEmpty();
        RuleFor(x => x.IntervalSeconds).InclusiveBetween(1, 3600);
        RuleFor(x => x.Fit).Must(v => AllowedFits.Contains(v)).WithMessage($"Fit must be one of: {string.Join(", ", AllowedFits)}");
    }
}

public class PollElementConfigValidator : AbstractValidator<PollElementConfig>
{
    public PollElementConfigValidator()
    {
        RuleFor(x => x.Question).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Options).NotEmpty();
        RuleForEach(x => x.Options).ChildRules(option =>
        {
            option.RuleFor(o => o.Label).NotEmpty().MaximumLength(80);
            option.RuleFor(o => o.Votes).GreaterThanOrEqualTo(0);
        });
        RuleFor(x => x.AccentColor).NotEmpty().MaximumLength(30);
        RuleFor(x => x.TextColor).NotEmpty().MaximumLength(30);
    }
}

public class ScoreboardElementConfigValidator : AbstractValidator<ScoreboardElementConfig>
{
    public ScoreboardElementConfigValidator()
    {
        RuleFor(x => x.TeamA).NotNull();
        RuleFor(x => x.TeamB).NotNull();
        RuleFor(x => x.TeamA.Name).NotEmpty().MaximumLength(40);
        RuleFor(x => x.TeamB.Name).NotEmpty().MaximumLength(40);
        RuleFor(x => x.TeamA.Score).GreaterThanOrEqualTo(0);
        RuleFor(x => x.TeamB.Score).GreaterThanOrEqualTo(0);
        RuleFor(x => x.TextColor).NotEmpty().MaximumLength(30);
    }
}

public class NowPlayingElementConfigValidator : AbstractValidator<NowPlayingElementConfig>
{
    public NowPlayingElementConfigValidator()
    {
        RuleFor(x => x.Track).NotNull().MaximumLength(120);
        RuleFor(x => x.Artist).NotNull().MaximumLength(120);
        RuleFor(x => x.TextColor).NotEmpty().MaximumLength(30);
    }
}

public class BrowserEmbedElementConfigValidator : AbstractValidator<BrowserEmbedElementConfig>
{
    public BrowserEmbedElementConfigValidator()
    {
        RuleFor(x => x.Url)
            .NotEmpty()
            .MaximumLength(2000)
            .Must(BeHttps)
            .WithMessage("Url must be a valid https:// address (this becomes a sandboxed iframe -- see the security note on BrowserEmbedElementConfig).");
    }

    private static bool BeHttps(string url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var parsed) && parsed.Scheme == Uri.UriSchemeHttps;
}

public class AlertElementConfigValidator : AbstractValidator<AlertElementConfig>
{
    private static readonly string[] AllowedStyles = ["fade", "slide"];

    public AlertElementConfigValidator()
    {
        RuleFor(x => x.Message).NotEmpty().MaximumLength(200);
        RuleFor(x => x.AccentColor).NotEmpty().MaximumLength(30);
        RuleFor(x => x.AnimationStyle).Must(v => AllowedStyles.Contains(v))
            .WithMessage($"AnimationStyle must be one of: {string.Join(", ", AllowedStyles)}");
    }
}
