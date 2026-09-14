using System.Text.Json;
using Beam.Modules.Elements.Validators;
using Beam.Shared.Exceptions;

namespace Beam.Modules.Elements.Contracts;

/// <summary>
/// The one place that knows how Element.Config's raw JSON maps to a strongly-typed shape per
/// ElementType. Everything else (DTOs, services, validators) works with the typed records.
/// </summary>
public static class ElementConfigSerializer
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static object Deserialize(ElementType type, string json) => type switch
    {
        ElementType.Text => JsonSerializer.Deserialize<TextElementConfig>(json, JsonOptions)!,
        ElementType.Image => JsonSerializer.Deserialize<ImageElementConfig>(json, JsonOptions)!,
        ElementType.Video => JsonSerializer.Deserialize<VideoElementConfig>(json, JsonOptions)!,
        ElementType.Camera => JsonSerializer.Deserialize<CameraElementConfig>(json, JsonOptions)!,
        ElementType.StatCounter => JsonSerializer.Deserialize<StatCounterElementConfig>(json, JsonOptions)!,
        ElementType.QrCode => JsonSerializer.Deserialize<QrCodeElementConfig>(json, JsonOptions)!,
        ElementType.Countdown => JsonSerializer.Deserialize<CountdownElementConfig>(json, JsonOptions)!,
        ElementType.ProgressBar => JsonSerializer.Deserialize<ProgressBarElementConfig>(json, JsonOptions)!,
        ElementType.Ticker => JsonSerializer.Deserialize<TickerElementConfig>(json, JsonOptions)!,
        ElementType.LowerThird => JsonSerializer.Deserialize<LowerThirdElementConfig>(json, JsonOptions)!,
        ElementType.ImageCarousel => JsonSerializer.Deserialize<ImageCarouselElementConfig>(json, JsonOptions)!,
        ElementType.Poll => JsonSerializer.Deserialize<PollElementConfig>(json, JsonOptions)!,
        ElementType.Scoreboard => JsonSerializer.Deserialize<ScoreboardElementConfig>(json, JsonOptions)!,
        ElementType.NowPlaying => JsonSerializer.Deserialize<NowPlayingElementConfig>(json, JsonOptions)!,
        ElementType.BrowserEmbed => JsonSerializer.Deserialize<BrowserEmbedElementConfig>(json, JsonOptions)!,
        ElementType.Alert => JsonSerializer.Deserialize<AlertElementConfig>(json, JsonOptions)!,
        _ => throw new ArgumentOutOfRangeException(nameof(type)),
    };

    public static object ParseAndValidate(ElementType type, JsonElement raw)
    {
        object config = type switch
        {
            ElementType.Text => Require(raw.Deserialize<TextElementConfig>(JsonOptions)),
            ElementType.Image => Require(raw.Deserialize<ImageElementConfig>(JsonOptions)),
            ElementType.Video => Require(raw.Deserialize<VideoElementConfig>(JsonOptions)),
            ElementType.Camera => Require(raw.Deserialize<CameraElementConfig>(JsonOptions)),
            ElementType.StatCounter => Require(raw.Deserialize<StatCounterElementConfig>(JsonOptions)),
            ElementType.QrCode => Require(raw.Deserialize<QrCodeElementConfig>(JsonOptions)),
            ElementType.Countdown => Require(raw.Deserialize<CountdownElementConfig>(JsonOptions)),
            ElementType.ProgressBar => Require(raw.Deserialize<ProgressBarElementConfig>(JsonOptions)),
            ElementType.Ticker => Require(raw.Deserialize<TickerElementConfig>(JsonOptions)),
            ElementType.LowerThird => Require(raw.Deserialize<LowerThirdElementConfig>(JsonOptions)),
            ElementType.ImageCarousel => Require(raw.Deserialize<ImageCarouselElementConfig>(JsonOptions)),
            ElementType.Poll => Require(raw.Deserialize<PollElementConfig>(JsonOptions)),
            ElementType.Scoreboard => Require(raw.Deserialize<ScoreboardElementConfig>(JsonOptions)),
            ElementType.NowPlaying => Require(raw.Deserialize<NowPlayingElementConfig>(JsonOptions)),
            ElementType.BrowserEmbed => Require(raw.Deserialize<BrowserEmbedElementConfig>(JsonOptions)),
            ElementType.Alert => Require(raw.Deserialize<AlertElementConfig>(JsonOptions)),
            _ => throw new ArgumentOutOfRangeException(nameof(type)),
        };

        var errors = config switch
        {
            TextElementConfig text => new TextElementConfigValidator().Validate(text).Errors,
            ImageElementConfig image => new ImageElementConfigValidator().Validate(image).Errors,
            VideoElementConfig video => new VideoElementConfigValidator().Validate(video).Errors,
            CameraElementConfig camera => new CameraElementConfigValidator().Validate(camera).Errors,
            StatCounterElementConfig stat => new StatCounterElementConfigValidator().Validate(stat).Errors,
            QrCodeElementConfig qr => new QrCodeElementConfigValidator().Validate(qr).Errors,
            CountdownElementConfig countdown => new CountdownElementConfigValidator().Validate(countdown).Errors,
            ProgressBarElementConfig progress => new ProgressBarElementConfigValidator().Validate(progress).Errors,
            TickerElementConfig ticker => new TickerElementConfigValidator().Validate(ticker).Errors,
            LowerThirdElementConfig lowerThird => new LowerThirdElementConfigValidator().Validate(lowerThird).Errors,
            ImageCarouselElementConfig carousel => new ImageCarouselElementConfigValidator().Validate(carousel).Errors,
            PollElementConfig poll => new PollElementConfigValidator().Validate(poll).Errors,
            ScoreboardElementConfig scoreboard => new ScoreboardElementConfigValidator().Validate(scoreboard).Errors,
            NowPlayingElementConfig nowPlaying => new NowPlayingElementConfigValidator().Validate(nowPlaying).Errors,
            BrowserEmbedElementConfig embed => new BrowserEmbedElementConfigValidator().Validate(embed).Errors,
            AlertElementConfig alert => new AlertElementConfigValidator().Validate(alert).Errors,
            _ => [],
        };

        if (errors.Count > 0)
        {
            throw new FluentValidation.ValidationException(errors);
        }

        return config;
    }

    private static T Require<T>(T? value) where T : class =>
        value ?? throw new ValidationAppException($"{typeof(T).Name} is required.");

    public static string Serialize(object config) => JsonSerializer.Serialize(config, JsonOptions);
}
