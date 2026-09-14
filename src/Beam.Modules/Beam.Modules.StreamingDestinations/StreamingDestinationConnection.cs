using Beam.Shared.Entities;

namespace Beam.Modules.StreamingDestinations;

public enum StreamingProvider
{
    Twitch = 0,
    YouTube = 1,
}

public enum StreamingOwnerType
{
    User = 0,
    Organization = 1,
}

/// <summary>
/// One connected channel. Tokens are encrypted at rest (ASP.NET Core Data Protection, see
/// StreamingDestinationService) and never serialized back to the frontend -- callers only ever
/// see StreamingConnectionDto, which carries a display name and status, not the tokens.
/// </summary>
public class StreamingDestinationConnection : EntityBase
{
    public StreamingOwnerType OwnerType { get; set; }

    public Guid? OwnerUserId { get; set; }

    public Guid? OwnerOrganizationId { get; set; }

    public StreamingProvider Provider { get; set; }

    public string ExternalAccountId { get; set; } = string.Empty;

    public string ExternalAccountName { get; set; } = string.Empty;

    public string EncryptedAccessToken { get; set; } = string.Empty;

    public string? EncryptedRefreshToken { get; set; }

    public DateTimeOffset? AccessTokenExpiresAtUtc { get; set; }

    public string Scopes { get; set; } = string.Empty;
}
