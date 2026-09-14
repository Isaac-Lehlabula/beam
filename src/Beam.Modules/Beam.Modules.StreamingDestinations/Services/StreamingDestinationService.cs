using Beam.Modules.StreamingDestinations.Abstractions;
using Beam.Modules.StreamingDestinations.Contracts;
using Beam.Shared.Abstractions;
using Beam.Shared.Exceptions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

namespace Beam.Modules.StreamingDestinations.Services;

/// <summary>
/// Orchestrates OAuth + token storage across whichever IStreamingPlatformIntegration matches
/// the requested provider. Tokens are encrypted with ASP.NET Core Data Protection before
/// touching the database and only ever decrypted in-process to make an outbound API call --
/// they're never included in any DTO returned to the frontend.
/// </summary>
public class StreamingDestinationService(
    IStreamingDestinationsDbContext db,
    IEnumerable<IStreamingPlatformIntegration> integrations,
    IDataProtectionProvider dataProtection,
    ICurrentUserContext currentUser) : IStreamingDestinationService
{
    private readonly IDataProtector protector = dataProtection.CreateProtector("Beam.StreamingDestinationTokens.v1");

    public string BuildAuthorizationUrl(StreamingProvider provider, string redirectUri, string state) =>
        Resolve(provider).BuildAuthorizationUrl(state, redirectUri);

    public async Task<StreamingConnectionDto> HandleCallbackAsync(StreamingProvider provider, OAuthCallbackRequest request, CancellationToken ct = default)
    {
        if (currentUser.UserId is null)
        {
            throw new ForbiddenAccessException("A signed-in user is required to connect a channel.");
        }

        var integration = Resolve(provider);
        var tokenResult = await integration.ExchangeCodeAsync(request.Code, request.RedirectUri, ct);
        var channel = await integration.GetChannelInfoAsync(tokenResult.AccessToken, ct);

        var existing = await db.StreamingDestinationConnections
            .FirstOrDefaultAsync(c => c.Provider == provider && c.OwnerUserId == currentUser.UserId, ct);

        var connection = existing ?? new StreamingDestinationConnection
        {
            OwnerType = StreamingOwnerType.User,
            OwnerUserId = currentUser.UserId,
            Provider = provider,
        };

        connection.ExternalAccountId = channel.ExternalAccountId;
        connection.ExternalAccountName = channel.DisplayName;
        connection.EncryptedAccessToken = protector.Protect(tokenResult.AccessToken);
        if (tokenResult.RefreshToken is not null)
        {
            connection.EncryptedRefreshToken = protector.Protect(tokenResult.RefreshToken);
        }
        connection.AccessTokenExpiresAtUtc = tokenResult.ExpiresAtUtc;
        connection.Scopes = tokenResult.Scopes;

        if (existing is null)
        {
            db.StreamingDestinationConnections.Add(connection);
        }

        await db.SaveChangesAsync(ct);
        return StreamingConnectionDto.FromEntity(connection);
    }

    public async Task<List<StreamingConnectionDto>> ListConnectionsAsync(CancellationToken ct = default) =>
        await db.StreamingDestinationConnections
            .Where(c => c.OwnerUserId == currentUser.UserId)
            .Select(c => StreamingConnectionDto.FromEntity(c))
            .ToListAsync(ct);

    public async Task<ViewerMetrics> GetViewerMetricsAsync(Guid connectionId, CancellationToken ct = default)
    {
        var connection = await FindOwnedConnectionAsync(connectionId, ct);
        var integration = Resolve(connection.Provider);
        var accessToken = await GetFreshAccessTokenAsync(connection, integration, ct);
        return await integration.GetViewerMetricsAsync(accessToken, connection.ExternalAccountId, ct);
    }

    public async Task DisconnectAsync(Guid connectionId, CancellationToken ct = default)
    {
        var connection = await FindOwnedConnectionAsync(connectionId, ct);
        db.StreamingDestinationConnections.Remove(connection);
        await db.SaveChangesAsync(ct);
    }

    private async Task<string> GetFreshAccessTokenAsync(StreamingDestinationConnection connection, IStreamingPlatformIntegration integration, CancellationToken ct)
    {
        var stillValid = connection.AccessTokenExpiresAtUtc is null || connection.AccessTokenExpiresAtUtc > DateTimeOffset.UtcNow.AddMinutes(1);
        if (stillValid)
        {
            return protector.Unprotect(connection.EncryptedAccessToken);
        }

        if (connection.EncryptedRefreshToken is null)
        {
            throw new ValidationAppException($"The {connection.Provider} connection has expired and has no refresh token; reconnect it.");
        }

        var refreshed = await integration.RefreshAsync(protector.Unprotect(connection.EncryptedRefreshToken), ct);
        connection.EncryptedAccessToken = protector.Protect(refreshed.AccessToken);
        // Some providers (Google) don't re-issue a refresh token on refresh -- keep the existing one unless a new one arrived.
        if (refreshed.RefreshToken is not null)
        {
            connection.EncryptedRefreshToken = protector.Protect(refreshed.RefreshToken);
        }
        connection.AccessTokenExpiresAtUtc = refreshed.ExpiresAtUtc;
        await db.SaveChangesAsync(ct);

        return refreshed.AccessToken;
    }

    private async Task<StreamingDestinationConnection> FindOwnedConnectionAsync(Guid connectionId, CancellationToken ct)
    {
        var connection = await db.StreamingDestinationConnections
            .FirstOrDefaultAsync(c => c.Id == connectionId && c.OwnerUserId == currentUser.UserId, ct);
        if (connection is null)
        {
            throw new NotFoundException(nameof(StreamingDestinationConnection), connectionId);
        }

        return connection;
    }

    private IStreamingPlatformIntegration Resolve(StreamingProvider provider) =>
        integrations.FirstOrDefault(i => i.Provider == provider)
            ?? throw new ValidationAppException($"No integration registered for provider '{provider}'.");
}
