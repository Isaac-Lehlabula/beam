namespace Beam.Modules.StreamingDestinations;

/// <summary>
/// Every app that does Twitch/YouTube OAuth needs its own registered client id/secret --
/// register a Twitch app at https://dev.twitch.tv/console/apps and a Google OAuth client at
/// https://console.cloud.google.com/apis/credentials, then set these via configuration
/// (Integrations:Twitch:ClientId etc., or the matching environment variables). Unset by
/// default; StreamingDestinationsController returns a clear error rather than a confusing
/// failure if a provider isn't configured.
/// </summary>
public class TwitchIntegrationOptions
{
    public string? ClientId { get; set; }

    public string? ClientSecret { get; set; }
}

public class YouTubeIntegrationOptions
{
    public string? ClientId { get; set; }

    public string? ClientSecret { get; set; }
}
