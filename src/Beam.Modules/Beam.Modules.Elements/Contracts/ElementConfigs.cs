namespace Beam.Modules.Elements.Contracts;

public record TextElementConfig(
    string Content,
    string FontFamily,
    int FontSize,
    string FontWeight,
    string TextAlign,
    string Color,
    string? BackgroundColor);

public record ImageElementConfig(Guid AssetId, string Fit);

public record VideoElementConfig(Guid AssetId, bool Loop, bool Muted, string Fit);

/// <summary>
/// Design-time-only: renders a labeled placeholder box in the editor so a designer can lay out
/// where a camera should go, but renders nothing (fully transparent) in the live overlay -- the
/// actual camera video comes from a separate OBS source composited on another layer, never
/// through our browser-source overlay. See SceneStage's Camera branch.
/// </summary>
public record CameraElementConfig(string Label, string BorderColor);

/// <summary>Kind is "viewers" | "subscribers" | "followers". Manual value until Phase 9 wires live platform data.</summary>
public record StatCounterElementConfig(string Kind, string Label, int ManualValue, string FontFamily, int FontSize, string Color);

public record QrCodeElementConfig(string Value, string ForegroundColor, string BackgroundColor);

/// <summary>EndsAtUtc is the server-anchored target so a reconnecting overlay computes the same remaining time -- never a client-only decrementing counter.</summary>
public record CountdownElementConfig(DateTimeOffset EndsAtUtc, string Label, string CompletedText, string FontFamily, int FontSize, string Color);

public record ProgressBarElementConfig(string Label, double CurrentValue, double TargetValue, string BarColor, string TrackColor, string TextColor);

public record TickerElementConfig(List<string> Items, int SpeedPxPerSecond, string BackgroundColor, string TextColor, int FontSize);

/// <summary>Covers both "lower third" and "guest name/title plate" from the brief -- same shape, same purpose.</summary>
public record LowerThirdElementConfig(string PrimaryText, string SecondaryText, string AccentColor, string TextColor, string BackgroundColor);

/// <summary>Covers both "image carousel" and rotating "sponsor graphics" from the brief.</summary>
public record ImageCarouselElementConfig(List<Guid> AssetIds, int IntervalSeconds, string Fit);

public record PollOption(string Label, int Votes);

public record PollElementConfig(string Question, List<PollOption> Options, string AccentColor, string TextColor);

public record ScoreboardTeam(string Name, int Score, string Color);

public record ScoreboardElementConfig(ScoreboardTeam TeamA, ScoreboardTeam TeamB, string TextColor);

public record NowPlayingElementConfig(string Track, string Artist, Guid? ArtworkAssetId, string TextColor);

/// <summary>
/// Sandboxed iframe -- the brief's explicit XSS concern for "custom text/HTML" elements.
/// Url is restricted to https:// (validator) and rendered with a locked-down sandbox attribute
/// (no same-origin, no top navigation, no forms) so an embedded page can't touch the parent
/// overlay or masquerade as it. See SceneStage's BrowserEmbed branch and its validator.
/// </summary>
public record BrowserEmbedElementConfig(string Url);

/// <summary>AnimationStyle is "fade" | "slide". Manually triggerable now; Phase 9's event stream can fire it automatically later.</summary>
public record AlertElementConfig(string Message, string AccentColor, string AnimationStyle);
