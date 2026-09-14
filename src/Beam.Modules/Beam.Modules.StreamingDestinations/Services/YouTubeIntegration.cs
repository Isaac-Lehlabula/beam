using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Web;
using Beam.Modules.StreamingDestinations.Abstractions;
using Beam.Shared.Exceptions;
using Microsoft.Extensions.Options;

namespace Beam.Modules.StreamingDestinations.Services;

/// <summary>
/// Google OAuth2 (https://developers.google.com/identity/protocols/oauth2/web-server) +
/// YouTube Data API v3 (https://developers.google.com/youtube/v3). access_type=offline +
/// prompt=consent on the authorize URL is what makes Google actually return a refresh_token --
/// omitting either is the most common reason a "working" YouTube OAuth integration silently
/// stops refreshing after the first access token expires.
/// </summary>
public class YouTubeIntegration(HttpClient httpClient, IOptions<YouTubeIntegrationOptions> options) : IStreamingPlatformIntegration
{
    private const string AuthorizeUrl = "https://accounts.google.com/o/oauth2/v2/auth";
    private const string TokenUrl = "https://oauth2.googleapis.com/token";
    private const string ApiBaseUrl = "https://www.googleapis.com/youtube/v3";
    private static readonly string[] Scopes = ["https://www.googleapis.com/auth/youtube.readonly"];

    public StreamingProvider Provider => StreamingProvider.YouTube;

    public string BuildAuthorizationUrl(string state, string redirectUri)
    {
        var clientId = RequireClientId();
        var query = HttpUtility.ParseQueryString(string.Empty);
        query["client_id"] = clientId;
        query["redirect_uri"] = redirectUri;
        query["response_type"] = "code";
        query["scope"] = string.Join(' ', Scopes);
        query["access_type"] = "offline";
        query["prompt"] = "consent";
        query["state"] = state;
        return $"{AuthorizeUrl}?{query}";
    }

    public async Task<OAuthExchangeResult> ExchangeCodeAsync(string code, string redirectUri, CancellationToken ct = default)
    {
        var (clientId, clientSecret) = RequireCredentials();
        var response = await httpClient.PostAsync(TokenUrl, new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_id"] = clientId,
            ["client_secret"] = clientSecret,
            ["code"] = code,
            ["grant_type"] = "authorization_code",
            ["redirect_uri"] = redirectUri,
        }), ct);

        return await ParseTokenResponseAsync(response, ct);
    }

    public async Task<OAuthExchangeResult> RefreshAsync(string refreshToken, CancellationToken ct = default)
    {
        var (clientId, clientSecret) = RequireCredentials();
        var response = await httpClient.PostAsync(TokenUrl, new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_id"] = clientId,
            ["client_secret"] = clientSecret,
            ["refresh_token"] = refreshToken,
            ["grant_type"] = "refresh_token",
        }), ct);

        // Google does not re-return refresh_token on a refresh response; callers must keep the
        // original one (see StreamingDestinationService.RefreshIfNeededAsync).
        return await ParseTokenResponseAsync(response, ct);
    }

    public async Task<ChannelInfo> GetChannelInfoAsync(string accessToken, CancellationToken ct = default)
    {
        var body = await GetAsync<YouTubeChannelsResponse>("/channels?part=snippet&mine=true", accessToken, ct);
        var channel = body.Items.FirstOrDefault() ?? throw new ValidationAppException("YouTube did not return a channel for this token.");
        return new ChannelInfo(channel.Id, channel.Snippet.Title);
    }

    public async Task<ViewerMetrics> GetViewerMetricsAsync(string accessToken, string externalAccountId, CancellationToken ct = default)
    {
        var channelBody = await GetAsync<YouTubeChannelsStatsResponse>($"/channels?part=statistics&id={externalAccountId}", accessToken, ct);
        var subscriberCount = channelBody.Items.FirstOrDefault()?.Statistics.SubscriberCount;

        var broadcastsBody = await GetAsync<YouTubeBroadcastsResponse>("/liveBroadcasts?part=id&broadcastStatus=active&mine=true", accessToken, ct);
        var activeBroadcastId = broadcastsBody.Items.FirstOrDefault()?.Id;
        if (activeBroadcastId is null)
        {
            return new ViewerMetrics(false, 0, null, subscriberCount);
        }

        var videoBody = await GetAsync<YouTubeVideosResponse>($"/videos?part=liveStreamingDetails&id={activeBroadcastId}", accessToken, ct);
        var concurrentViewers = videoBody.Items.FirstOrDefault()?.LiveStreamingDetails?.ConcurrentViewers ?? 0;

        return new ViewerMetrics(true, concurrentViewers, null, subscriberCount);
    }

    private async Task<T> GetAsync<T>(string path, string accessToken, CancellationToken ct)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"{ApiBaseUrl}{path}");
        request.Headers.Add("Authorization", $"Bearer {accessToken}");
        var response = await httpClient.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(cancellationToken: ct)
            ?? throw new ValidationAppException("YouTube returned an empty response.");
    }

    private async Task<OAuthExchangeResult> ParseTokenResponseAsync(HttpResponseMessage response, CancellationToken ct)
    {
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new ValidationAppException($"Google rejected the token request: {body}");
        }

        var token = await response.Content.ReadFromJsonAsync<GoogleTokenResponse>(cancellationToken: ct)
            ?? throw new ValidationAppException("Google returned an empty token response.");
        return new OAuthExchangeResult(
            token.AccessToken,
            token.RefreshToken,
            token.ExpiresIn.HasValue ? DateTimeOffset.UtcNow.AddSeconds(token.ExpiresIn.Value) : null,
            token.Scope ?? string.Empty);
    }

    private string RequireClientId() =>
        string.IsNullOrWhiteSpace(options.Value.ClientId)
            ? throw new ValidationAppException("YouTube integration is not configured (missing Integrations:YouTube:ClientId).")
            : options.Value.ClientId;

    private (string ClientId, string ClientSecret) RequireCredentials()
    {
        var clientId = RequireClientId();
        if (string.IsNullOrWhiteSpace(options.Value.ClientSecret))
        {
            throw new ValidationAppException("YouTube integration is not configured (missing Integrations:YouTube:ClientSecret).");
        }

        return (clientId, options.Value.ClientSecret);
    }

    private record GoogleTokenResponse(
        [property: JsonPropertyName("access_token")] string AccessToken,
        [property: JsonPropertyName("refresh_token")] string? RefreshToken,
        [property: JsonPropertyName("expires_in")] int? ExpiresIn,
        [property: JsonPropertyName("scope")] string? Scope);

    private record YouTubeChannelsResponse([property: JsonPropertyName("items")] List<YouTubeChannel> Items);

    private record YouTubeChannel(
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("snippet")] YouTubeChannelSnippet Snippet);

    private record YouTubeChannelSnippet([property: JsonPropertyName("title")] string Title);

    private record YouTubeChannelsStatsResponse([property: JsonPropertyName("items")] List<YouTubeChannelStats> Items);

    private record YouTubeChannelStats([property: JsonPropertyName("statistics")] YouTubeStatistics Statistics);

    private record YouTubeStatistics([property: JsonPropertyName("subscriberCount")] int SubscriberCount);

    private record YouTubeBroadcastsResponse([property: JsonPropertyName("items")] List<YouTubeBroadcast> Items);

    private record YouTubeBroadcast([property: JsonPropertyName("id")] string Id);

    private record YouTubeVideosResponse([property: JsonPropertyName("items")] List<YouTubeVideo> Items);

    private record YouTubeVideo([property: JsonPropertyName("liveStreamingDetails")] YouTubeLiveStreamingDetails? LiveStreamingDetails);

    private record YouTubeLiveStreamingDetails([property: JsonPropertyName("concurrentViewers")] int? ConcurrentViewers);
}
