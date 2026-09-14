using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Web;
using Beam.Modules.StreamingDestinations.Abstractions;
using Beam.Shared.Exceptions;
using Microsoft.Extensions.Options;

namespace Beam.Modules.StreamingDestinations.Services;

/// <summary>
/// Twitch OAuth (https://dev.twitch.tv/docs/authentication/) + Helix API
/// (https://dev.twitch.tv/docs/api/reference/). Every Helix call needs both a user access token
/// (Authorization: Bearer) AND the app's Client-Id header -- easy to miss, called out here
/// because a missing Client-Id fails with an unhelpful 401 that looks like a bad token.
/// </summary>
public class TwitchIntegration(HttpClient httpClient, IOptions<TwitchIntegrationOptions> options) : IStreamingPlatformIntegration
{
    private const string AuthorizeUrl = "https://id.twitch.tv/oauth2/authorize";
    private const string TokenUrl = "https://id.twitch.tv/oauth2/token";
    private const string HelixBaseUrl = "https://api.twitch.tv/helix";
    private static readonly string[] Scopes = ["channel:read:subscriptions", "moderator:read:followers"];

    public StreamingProvider Provider => StreamingProvider.Twitch;

    public string BuildAuthorizationUrl(string state, string redirectUri)
    {
        var clientId = RequireClientId();
        var query = HttpUtility.ParseQueryString(string.Empty);
        query["client_id"] = clientId;
        query["redirect_uri"] = redirectUri;
        query["response_type"] = "code";
        query["scope"] = string.Join(' ', Scopes);
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

        return await ParseTokenResponseAsync(response, ct);
    }

    public async Task<ChannelInfo> GetChannelInfoAsync(string accessToken, CancellationToken ct = default)
    {
        var response = await SendHelixAsync(HttpMethod.Get, "/users", accessToken, ct);
        var body = await response.Content.ReadFromJsonAsync<TwitchUsersResponse>(cancellationToken: ct);
        var user = body?.Data.FirstOrDefault() ?? throw new ValidationAppException("Twitch did not return a user for this token.");
        return new ChannelInfo(user.Id, user.DisplayName);
    }

    public async Task<ViewerMetrics> GetViewerMetricsAsync(string accessToken, string externalAccountId, CancellationToken ct = default)
    {
        var streamResponse = await SendHelixAsync(HttpMethod.Get, $"/streams?user_id={externalAccountId}", accessToken, ct);
        var streamBody = await streamResponse.Content.ReadFromJsonAsync<TwitchStreamsResponse>(cancellationToken: ct);
        var liveStream = streamBody?.Data.FirstOrDefault();

        int? followerCount = null;
        try
        {
            var followersResponse = await SendHelixAsync(HttpMethod.Get, $"/channels/followers?broadcaster_id={externalAccountId}&first=1", accessToken, ct);
            var followersBody = await followersResponse.Content.ReadFromJsonAsync<TwitchTotalResponse>(cancellationToken: ct);
            followerCount = followersBody?.Total;
        }
        catch
        {
            // Requires moderator:read:followers scope on the broadcaster's own token; if the
            // user declined that scope we simply omit the count rather than failing the whole call.
        }

        return new ViewerMetrics(liveStream is not null, liveStream?.ViewerCount ?? 0, followerCount, null);
    }

    private async Task<HttpResponseMessage> SendHelixAsync(HttpMethod method, string path, string accessToken, CancellationToken ct)
    {
        var request = new HttpRequestMessage(method, $"{HelixBaseUrl}{path}");
        request.Headers.Add("Authorization", $"Bearer {accessToken}");
        request.Headers.Add("Client-Id", RequireClientId());
        var response = await httpClient.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
        return response;
    }

    private async Task<OAuthExchangeResult> ParseTokenResponseAsync(HttpResponseMessage response, CancellationToken ct)
    {
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new ValidationAppException($"Twitch rejected the token request: {body}");
        }

        var token = await response.Content.ReadFromJsonAsync<TwitchTokenResponse>(cancellationToken: ct)
            ?? throw new ValidationAppException("Twitch returned an empty token response.");
        return new OAuthExchangeResult(
            token.AccessToken,
            token.RefreshToken,
            token.ExpiresIn.HasValue ? DateTimeOffset.UtcNow.AddSeconds(token.ExpiresIn.Value) : null,
            string.Join(' ', token.Scope ?? []));
    }

    private string RequireClientId() =>
        string.IsNullOrWhiteSpace(options.Value.ClientId)
            ? throw new ValidationAppException("Twitch integration is not configured (missing Integrations:Twitch:ClientId).")
            : options.Value.ClientId;

    private (string ClientId, string ClientSecret) RequireCredentials()
    {
        var clientId = RequireClientId();
        if (string.IsNullOrWhiteSpace(options.Value.ClientSecret))
        {
            throw new ValidationAppException("Twitch integration is not configured (missing Integrations:Twitch:ClientSecret).");
        }

        return (clientId, options.Value.ClientSecret);
    }

    private record TwitchTokenResponse(
        [property: JsonPropertyName("access_token")] string AccessToken,
        [property: JsonPropertyName("refresh_token")] string? RefreshToken,
        [property: JsonPropertyName("expires_in")] int? ExpiresIn,
        [property: JsonPropertyName("scope")] string[]? Scope);

    private record TwitchUsersResponse([property: JsonPropertyName("data")] List<TwitchUser> Data);

    private record TwitchUser(
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("display_name")] string DisplayName);

    private record TwitchStreamsResponse([property: JsonPropertyName("data")] List<TwitchStream> Data);

    private record TwitchStream([property: JsonPropertyName("viewer_count")] int ViewerCount);

    private record TwitchTotalResponse([property: JsonPropertyName("total")] int Total);
}
