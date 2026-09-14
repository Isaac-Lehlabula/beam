using System.Text.Json;
using System.Text.Json.Serialization;
using Beam.Modules.AiGeneration.Abstractions;
using Beam.Modules.AiGeneration.Contracts;
using Beam.Modules.Elements;
using Beam.Modules.Elements.Contracts;
using Beam.Modules.Projects.Contracts;
using Beam.Modules.Templates.Abstractions;
using Beam.Modules.Templates.Contracts;
using Beam.Shared.Exceptions;

namespace Beam.Modules.AiGeneration.Services;

/// <summary>
/// Prompt -> Template-shaped JSON -> the exact same instantiation pipeline Phase 5's template
/// gallery uses (ITemplateService.InstantiateDefinitionAsync) -- see ARCHITECTURE.md §11. The
/// generated result is a normal, fully-editable project from the moment it's created; there is
/// no separate "AI mode" data model.
///
/// Deliberately restricted to Text and LowerThird elements (see AllowedTypes): asking a model to
/// reliably produce all sixteen element config shapes freeform is a much larger prompt-engineering
/// problem than this pass takes on, and a themed multi-scene project built from just those two
/// is already the brief's full "football podcast" example end to end.
/// </summary>
public class AiProjectGenerationService(IAnthropicClient anthropicClient, ITemplateService templateService) : IAiProjectGenerationService
{
    private static readonly ElementType[] AllowedTypes = [ElementType.Text, ElementType.LowerThird];

    private static readonly JsonSerializerOptions ResponseJsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    public async Task<ProjectDto> GenerateProjectAsync(GenerateProjectRequest request, CancellationToken ct = default)
    {
        var raw = await anthropicClient.CompleteAsync(BuildSystemPrompt(request.ResolutionWidth, request.ResolutionHeight), request.Prompt, ct);
        var definition = ParseAndValidateDefinition(raw);

        var projectName = ExtractProjectName(request.Prompt);
        return await templateService.InstantiateDefinitionAsync(definition, projectName, request.ResolutionWidth, request.ResolutionHeight, organizationId: null, ct);
    }

    private static string BuildSystemPrompt(int width, int height) => $$"""
        You design livestream overlay layouts. Given a description of a show, output ONLY a JSON
        object (no markdown fences, no commentary) with this exact shape:

        {
          "scenes": [
            {
              "name": "string, e.g. 'Starting Soon', 'Main', 'Break', 'Ending'",
              "elements": [
                {
                  "type": "Text" | "LowerThird",
                  "name": "short internal label",
                  "x": number, "y": number, "width": number, "height": number,
                  "rotation": 0, "opacity": 1,
                  "config": <see below, matching "type">
                }
              ]
            }
          ]
        }

        Canvas is {{width}}x{{height}} px; keep every element's x/y/width/height inside those bounds.

        For "type": "Text", "config" must be:
        { "content": string, "fontFamily": string, "fontSize": number, "fontWeight": "400"|"600"|"700"|"900",
          "textAlign": "left"|"center"|"right", "color": "#rrggbb", "backgroundColor": null | "#rrggbb" }

        For "type": "LowerThird", "config" must be:
        { "primaryText": string, "secondaryText": string, "accentColor": "#rrggbb", "textColor": "#rrggbb", "backgroundColor": "#rrggbb or rgba(...)" }

        Produce 4-6 scenes covering a typical show flow (starting soon, main content, break,
        ending, and any scene types implied by the description, e.g. a multi-person discussion
        layout). Use a consistent color theme reflecting the description. Output raw JSON only.
        """;

    private TemplateDefinition ParseAndValidateDefinition(string raw)
    {
        var json = StripMarkdownFence(raw);

        AiTemplateDefinition parsed;
        try
        {
            parsed = JsonSerializer.Deserialize<AiTemplateDefinition>(json, ResponseJsonOptions)
                ?? throw new ValidationAppException("The AI response did not contain a usable layout.");
        }
        catch (JsonException ex)
        {
            throw new ValidationAppException($"The AI response was not valid JSON: {ex.Message}");
        }

        if (parsed.Scenes.Count == 0)
        {
            throw new ValidationAppException("The AI response did not include any scenes.");
        }

        var scenes = new List<TemplateSceneDefinition>();
        foreach (var scene in parsed.Scenes)
        {
            var elements = new List<TemplateElementDefinition>();
            foreach (var element in scene.Elements)
            {
                if (!AllowedTypes.Contains(element.Type))
                {
                    throw new ValidationAppException(
                        $"The AI generated an unsupported element type '{element.Type}' on scene '{scene.Name}'.");
                }

                // Reuses the exact same per-type validators every hand-authored element goes
                // through (ElementService.ParseAndValidateConfigAsync) -- this is the schema
                // guard the roadmap calls for, not a bespoke one for AI output.
                object validatedConfig;
                try
                {
                    validatedConfig = ElementConfigSerializer.ParseAndValidate(element.Type, element.Config);
                }
                catch (Exception ex) when (ex is FluentValidation.ValidationException or ValidationAppException)
                {
                    throw new ValidationAppException(
                        $"The AI generated an invalid '{element.Type}' element ('{element.Name}' on scene '{scene.Name}'): {ex.Message}");
                }

                elements.Add(new TemplateElementDefinition(
                    element.Type, element.Name, element.X, element.Y, element.Width, element.Height,
                    element.Rotation, element.Opacity,
                    JsonSerializer.SerializeToElement(validatedConfig, ResponseJsonOptions),
                    EditableProperties: null));
            }

            scenes.Add(new TemplateSceneDefinition(scene.Name, elements));
        }

        return new TemplateDefinition(scenes);
    }

    private static string StripMarkdownFence(string text)
    {
        var trimmed = text.Trim();
        if (!trimmed.StartsWith("```"))
        {
            return trimmed;
        }

        var firstNewline = trimmed.IndexOf('\n');
        var withoutOpeningFence = firstNewline >= 0 ? trimmed[(firstNewline + 1)..] : trimmed;
        var closingFenceIndex = withoutOpeningFence.LastIndexOf("```", StringComparison.Ordinal);
        return closingFenceIndex >= 0 ? withoutOpeningFence[..closingFenceIndex].Trim() : withoutOpeningFence.Trim();
    }

    private static string ExtractProjectName(string prompt) =>
        prompt.Length <= 60 ? prompt : string.Concat(prompt.AsSpan(0, 57), "...");

    private record AiTemplateDefinition(List<AiSceneDefinition> Scenes);

    private record AiSceneDefinition(string Name, List<AiElementDefinition> Elements);

    private record AiElementDefinition(
        ElementType Type, string Name, double X, double Y, double Width, double Height,
        double Rotation, double Opacity, JsonElement Config);
}
