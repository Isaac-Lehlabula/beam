using System.Text.Json;
using Beam.Modules.Elements;
using Beam.Modules.Elements.Contracts;
using Beam.Modules.Templates.Contracts;

namespace Beam.Modules.Templates.Seed;

/// <summary>
/// Three starter templates seeded via EF Core migration `HasData` (deterministic, hardcoded
/// ids so the seed is idempotent across environments). Built with Text elements only -- Image
/// elements need an uploaded asset, and a system-scoped template has no user to own one; richer
/// seed art can be layered on once Phase 4's broader element library exists.
/// </summary>
public static class TemplateSeedData
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static readonly Guid GamingTemplateId = Guid.Parse("00000000-0000-0000-0001-000000000001");
    public static readonly Guid PodcastTemplateId = Guid.Parse("00000000-0000-0000-0001-000000000002");
    public static readonly Guid JustChattingTemplateId = Guid.Parse("00000000-0000-0000-0001-000000000003");

    // EF Core's HasData bakes these values into the migration at generation time, so they must
    // be static and deterministic -- EntityBase's DateTimeOffset.UtcNow default would make the
    // model "change" on every build (EF detects this and refuses to migrate).
    private static readonly DateTimeOffset SeedTimestamp = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public static IEnumerable<Template> GetSeedTemplates()
    {
        yield return new Template
        {
            Id = GamingTemplateId,
            Name = "Gaming",
            Category = TemplateCategory.Gaming,
            Scope = TemplateScope.System,
            CreatedAtUtc = SeedTimestamp,
            UpdatedAtUtc = SeedTimestamp,
            DefinitionJson = Serialize(BuildDefinition(
                accentColor: "#a855f7",
                showTitle: "GAME NIGHT",
                subtitle: "Live now")),
        };

        yield return new Template
        {
            Id = PodcastTemplateId,
            Name = "Podcast",
            Category = TemplateCategory.Podcast,
            Scope = TemplateScope.System,
            CreatedAtUtc = SeedTimestamp,
            UpdatedAtUtc = SeedTimestamp,
            DefinitionJson = Serialize(BuildDefinition(
                accentColor: "#f59e0b",
                showTitle: "THE PODCAST",
                subtitle: "Episode recording")),
        };

        yield return new Template
        {
            Id = JustChattingTemplateId,
            Name = "Just Chatting",
            Category = TemplateCategory.JustChatting,
            Scope = TemplateScope.System,
            CreatedAtUtc = SeedTimestamp,
            UpdatedAtUtc = SeedTimestamp,
            DefinitionJson = Serialize(BuildDefinition(
                accentColor: "#22c55e",
                showTitle: "JUST CHATTING",
                subtitle: "Hanging out")),
        };
    }

    private static TemplateDefinition BuildDefinition(string accentColor, string showTitle, string subtitle) => new([
        new TemplateSceneDefinition(
            "Starting Soon",
            [
                TitleText(showTitle, accentColor, y: 420),
                SubtitleText("Starting soon...", y: 520),
            ]),
        new TemplateSceneDefinition(
            "Main",
            [
                LowerThirdText(showTitle, accentColor),
            ]),
        new TemplateSceneDefinition(
            "Break",
            [
                TitleText("BE RIGHT BACK", accentColor, y: 460),
                SubtitleText(subtitle, y: 560),
            ]),
        new TemplateSceneDefinition(
            "Ending",
            [
                TitleText("THANKS FOR WATCHING", accentColor, y: 460),
                SubtitleText("See you next time!", y: 560),
            ]),
    ]);

    private static TemplateElementDefinition TitleText(string content, string color, double y) => new(
        ElementType.Text, "Title", X: 260, Y: y, Width: 1400, Height: 120, Rotation: 0, Opacity: 1,
        Config: ToConfig(new TextElementConfig(content, "Inter, sans-serif", 72, "800", "center", color, null)));

    private static TemplateElementDefinition SubtitleText(string content, double y) => new(
        ElementType.Text, "Subtitle", X: 260, Y: y, Width: 1400, Height: 60, Rotation: 0, Opacity: 1,
        Config: ToConfig(new TextElementConfig(content, "Inter, sans-serif", 28, "500", "center", "#e5e5e5", null)));

    private static TemplateElementDefinition LowerThirdText(string content, string color) => new(
        ElementType.Text, "Lower Third", X: 60, Y: 900, Width: 700, Height: 90, Rotation: 0, Opacity: 1,
        Config: ToConfig(new TextElementConfig(content, "Inter, sans-serif", 36, "700", "left", "#ffffff", color)));

    private static JsonElement ToConfig(TextElementConfig config) => JsonSerializer.SerializeToElement(config, JsonOptions);

    private static string Serialize(TemplateDefinition definition) => JsonSerializer.Serialize(definition, JsonOptions);
}
