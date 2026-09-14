using Beam.Modules.StreamingDestinations.Abstractions;

namespace Beam.Modules.StreamingDestinations.Contracts;

/// <summary>Never carries tokens -- see StreamingDestinationConnection's doc comment.</summary>
public record StreamingConnectionDto(Guid Id, StreamingProvider Provider, string ExternalAccountName, DateTimeOffset CreatedAtUtc)
{
    public static StreamingConnectionDto FromEntity(StreamingDestinationConnection connection) =>
        new(connection.Id, connection.Provider, connection.ExternalAccountName, connection.CreatedAtUtc);
}

public record AuthorizationUrlDto(string Url);

/// <summary>
/// No `state` here deliberately: the SPA generates and verifies the CSRF state entirely
/// client-side (stored in sessionStorage before redirecting to the provider, checked against
/// the provider's redirect-back before this endpoint is ever called) -- see the frontend OAuth
/// callback page. The backend only needs a valid `code` to exchange.
/// </summary>
public record OAuthCallbackRequest(string Code, string RedirectUri);
