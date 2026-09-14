using System.Text.Json;
using Beam.Modules.Elements;

namespace Beam.Modules.Templates.Contracts;

/// <summary>The serialized shape stored in Template.DefinitionJson.</summary>
public record TemplateDefinition(List<TemplateSceneDefinition> Scenes);

public record TemplateSceneDefinition(string Name, List<TemplateElementDefinition> Elements);

/// <summary>
/// Config is kept as JsonElement (not a typed record) so this definition round-trips through
/// JSON without needing polymorphic deserialization -- ElementConfigSerializer (Elements module)
/// already knows how to turn a (Type, JsonElement) pair into the right typed config.
/// EditableProperties null means "fully editable"; see Element.EditableProperties for the lock semantics.
/// </summary>
public record TemplateElementDefinition(
    ElementType Type,
    string Name,
    double X,
    double Y,
    double Width,
    double Height,
    double Rotation,
    double Opacity,
    JsonElement Config,
    List<string>? EditableProperties = null);
