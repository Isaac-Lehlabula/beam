using System.Text.Json;
using Beam.Modules.Elements;
using Beam.Modules.Elements.Contracts;
using FluentAssertions;
using FluentValidation;
using Xunit;

namespace Beam.Api.Tests;

public class ExpandedElementConfigTests
{
    private static JsonElement ToJsonElement(object value) => JsonSerializer.SerializeToElement(value);

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("http://example.com")] // plain http, not https
    [InlineData("data:text/html,<script>alert(1)</script>")]
    [InlineData("not a url")]
    public void BrowserEmbed_rejects_anything_that_is_not_a_valid_https_url(string url)
    {
        var config = new BrowserEmbedElementConfig(url);

        var act = () => ElementConfigSerializer.ParseAndValidate(ElementType.BrowserEmbed, ToJsonElement(config));

        act.Should().Throw<ValidationException>();
    }

    [Fact]
    public void BrowserEmbed_accepts_a_valid_https_url()
    {
        var config = new BrowserEmbedElementConfig("https://example.com/widget");

        var parsed = ElementConfigSerializer.ParseAndValidate(ElementType.BrowserEmbed, ToJsonElement(config));

        parsed.Should().BeEquivalentTo(config);
    }

    [Fact]
    public void Countdown_config_round_trips_with_server_anchored_timestamp()
    {
        var endsAt = DateTimeOffset.UtcNow.AddMinutes(5);
        var config = new CountdownElementConfig(endsAt, "Starting in", "We're live!", "Inter", 48, "#fff");

        var parsed = (CountdownElementConfig)ElementConfigSerializer.ParseAndValidate(ElementType.Countdown, ToJsonElement(config));

        parsed.EndsAtUtc.Should().Be(endsAt);
    }

    [Fact]
    public void StatCounter_rejects_an_unknown_kind()
    {
        var config = new StatCounterElementConfig("subscribers-but-misspelled", "Subs", 100, "Inter", 24, "#fff");

        var act = () => ElementConfigSerializer.ParseAndValidate(ElementType.StatCounter, ToJsonElement(config));

        act.Should().Throw<ValidationException>();
    }

    [Fact]
    public void Poll_requires_at_least_one_option()
    {
        var config = new PollElementConfig("Who wins?", [], "#000", "#fff");

        var act = () => ElementConfigSerializer.ParseAndValidate(ElementType.Poll, ToJsonElement(config));

        act.Should().Throw<ValidationException>();
    }
}
