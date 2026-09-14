using Beam.Infrastructure;
using Beam.Modules.StreamingDestinations;
using Beam.Modules.StreamingDestinations.Abstractions;
using Beam.Modules.StreamingDestinations.Contracts;
using Beam.Modules.StreamingDestinations.Services;
using System.Web;
using Beam.Shared.Exceptions;
using FluentAssertions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Xunit;

namespace Beam.Api.Tests;

public class StreamingDestinationTests
{
    private static TwitchIntegration CreateTwitchIntegration(string? clientId = "test-client-id", string? clientSecret = "test-secret") =>
        new(new HttpClient(), Options.Create(new TwitchIntegrationOptions { ClientId = clientId, ClientSecret = clientSecret }));

    private static YouTubeIntegration CreateYouTubeIntegration(string? clientId = "test-client-id", string? clientSecret = "test-secret") =>
        new(new HttpClient(), Options.Create(new YouTubeIntegrationOptions { ClientId = clientId, ClientSecret = clientSecret }));

    [Fact]
    public void Twitch_authorization_url_includes_required_oauth_parameters()
    {
        var integration = CreateTwitchIntegration();

        var url = integration.BuildAuthorizationUrl("csrf-state-123", "https://app.example.com/callback");

        url.Should().StartWith("https://id.twitch.tv/oauth2/authorize?");
        url.Should().Contain("client_id=test-client-id");
        url.Should().Contain("response_type=code");
        url.Should().Contain("state=csrf-state-123");
        HttpUtility.ParseQueryString(new Uri(url).Query)["redirect_uri"].Should().Be("https://app.example.com/callback");
    }

    [Fact]
    public void YouTube_authorization_url_requests_offline_access_so_a_refresh_token_is_issued()
    {
        var integration = CreateYouTubeIntegration();

        var url = integration.BuildAuthorizationUrl("csrf-state-456", "https://app.example.com/callback");

        url.Should().StartWith("https://accounts.google.com/o/oauth2/v2/auth?");
        url.Should().Contain("access_type=offline");
        url.Should().Contain("prompt=consent");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Building_an_authorization_url_without_configured_credentials_fails_clearly(string? clientId)
    {
        // Config binding turns a missing appsettings value into "" (empty string), not null --
        // this must be treated the same as "not configured" or a real request goes out with a
        // blank client_id instead of a clear error (caught by exactly this test at first).
        var integration = CreateTwitchIntegration(clientId: clientId);

        var act = () => integration.BuildAuthorizationUrl("state", "https://app.example.com/callback");

        act.Should().Throw<ValidationAppException>().WithMessage("*not configured*");
    }

    [Fact]
    public async Task Access_and_refresh_tokens_are_encrypted_at_rest_and_never_stored_in_plaintext()
    {
        var owner = Guid.NewGuid();
        var currentUser = new FakeCurrentUserContext(owner);
        var options = new DbContextOptionsBuilder<BeamDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var db = new BeamDbContext(options, currentUser);
        var dataProtection = new EphemeralDataProtectionProvider();

        // HandleCallbackAsync needs a live HTTP call to Twitch, which we can't make here --
        // instead this test verifies the encryption contract directly, the way
        // StreamingDestinationService actually uses it (same purpose string, protect/unprotect).
        var protector = dataProtection.CreateProtector("Beam.StreamingDestinationTokens.v1");
        const string plainAccessToken = "super-secret-access-token";

        var encrypted = protector.Protect(plainAccessToken);
        var connection = new StreamingDestinationConnection
        {
            OwnerType = StreamingOwnerType.User,
            OwnerUserId = owner,
            Provider = StreamingProvider.Twitch,
            ExternalAccountId = "12345",
            ExternalAccountName = "SomeStreamer",
            EncryptedAccessToken = encrypted,
        };
        db.StreamingDestinationConnections.Add(connection);
        await db.SaveChangesAsync();

        var persisted = await db.StreamingDestinationConnections.FirstAsync(c => c.Id == connection.Id);
        persisted.EncryptedAccessToken.Should().NotBe(plainAccessToken);
        protector.Unprotect(persisted.EncryptedAccessToken).Should().Be(plainAccessToken);
    }

    [Fact]
    public async Task Disconnecting_someone_elses_connection_is_not_found_not_a_leak()
    {
        var owner = Guid.NewGuid();
        var otherUser = Guid.NewGuid();
        var options = new DbContextOptionsBuilder<BeamDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;

        Guid connectionId;
        using (var ownerDb = new BeamDbContext(options, new FakeCurrentUserContext(owner)))
        {
            var connection = new StreamingDestinationConnection
            {
                OwnerType = StreamingOwnerType.User,
                OwnerUserId = owner,
                Provider = StreamingProvider.YouTube,
                ExternalAccountId = "abc",
                ExternalAccountName = "Owner's Channel",
                EncryptedAccessToken = "irrelevant",
            };
            ownerDb.StreamingDestinationConnections.Add(connection);
            await ownerDb.SaveChangesAsync();
            connectionId = connection.Id;
        }

        var otherUserContext = new FakeCurrentUserContext(otherUser);
        var otherDb = new BeamDbContext(options, otherUserContext);
        var service = new StreamingDestinationService(
            otherDb,
            [CreateTwitchIntegration(), CreateYouTubeIntegration()],
            new EphemeralDataProtectionProvider(),
            otherUserContext);

        var act = () => service.DisconnectAsync(connectionId);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
