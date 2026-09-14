namespace Beam.Modules.StreamingDestinations.Abstractions;

public record OAuthExchangeResult(string AccessToken, string? RefreshToken, DateTimeOffset? ExpiresAtUtc, string Scopes);

public record ChannelInfo(string ExternalAccountId, string DisplayName);

/// <summary>
/// Normalized shape every element (Chat/StatCounter) consumes, regardless of provider --
/// ARCHITECTURE.md §11's point that the UI never needs to know whether data came from Twitch
/// or YouTube.
/// </summary>
public record ViewerMetrics(bool IsLive, int ViewerCount, int? FollowerCount, int? SubscriberCount);

/// <summary>
/// The server-side counterpart to Beam.Api's IBroadcastSoftwareIntegration (browser-side, for
/// OBS): this one is entirely server-side because it's OAuth against a cloud API, not a
/// localhost connection. See ARCHITECTURE.md §11 for why the two integration shapes are
/// deliberately different and never combined.
/// </summary>
public interface IStreamingPlatformIntegration
{
    StreamingProvider Provider { get; }

    string BuildAuthorizationUrl(string state, string redirectUri);

    Task<OAuthExchangeResult> ExchangeCodeAsync(string code, string redirectUri, CancellationToken ct = default);

    Task<OAuthExchangeResult> RefreshAsync(string refreshToken, CancellationToken ct = default);

    Task<ChannelInfo> GetChannelInfoAsync(string accessToken, CancellationToken ct = default);

    Task<ViewerMetrics> GetViewerMetricsAsync(string accessToken, string externalAccountId, CancellationToken ct = default);
}
