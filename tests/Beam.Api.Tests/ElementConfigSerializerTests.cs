using System.Text.Json;
using Beam.Modules.Elements;
using Beam.Modules.Elements.Contracts;
using FluentAssertions;
using FluentValidation;
using Xunit;

namespace Beam.Api.Tests;

public class ElementConfigSerializerTests
{
    private static JsonElement ToJsonElement(object value) => JsonSerializer.SerializeToElement(value);

    [Fact]
    public void Valid_text_config_round_trips()
    {
        var config = new TextElementConfig("Hello", "Inter", 32, "700", "left", "#fff", null);

        var parsed = ElementConfigSerializer.ParseAndValidate(ElementType.Text, ToJsonElement(config));

        parsed.Should().BeEquivalentTo(config);
    }

    [Fact]
    public void Text_config_with_invalid_alignment_is_rejected()
    {
        var config = new TextElementConfig("Hello", "Inter", 32, "700", "diagonal", "#fff", null);

        var act = () => ElementConfigSerializer.ParseAndValidate(ElementType.Text, ToJsonElement(config));

        act.Should().Throw<ValidationException>();
    }

    [Fact]
    public void Text_config_with_zero_font_size_is_rejected()
    {
        var config = new TextElementConfig("Hello", "Inter", 0, "700", "left", "#fff", null);

        var act = () => ElementConfigSerializer.ParseAndValidate(ElementType.Text, ToJsonElement(config));

        act.Should().Throw<ValidationException>();
    }

    [Fact]
    public void Image_config_with_invalid_fit_is_rejected()
    {
        var config = new ImageElementConfig(Guid.NewGuid(), "stretch");

        var act = () => ElementConfigSerializer.ParseAndValidate(ElementType.Image, ToJsonElement(config));

        act.Should().Throw<ValidationException>();
    }

    [Fact]
    public void Image_config_with_empty_asset_id_is_rejected()
    {
        var config = new ImageElementConfig(Guid.Empty, "cover");

        var act = () => ElementConfigSerializer.ParseAndValidate(ElementType.Image, ToJsonElement(config));

        act.Should().Throw<ValidationException>();
    }
}
