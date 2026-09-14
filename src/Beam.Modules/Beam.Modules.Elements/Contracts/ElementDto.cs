using System.Text.Json;

namespace Beam.Modules.Elements.Contracts;

public record ElementDto(
    Guid Id,
    Guid SceneId,
    ElementType Type,
    string Name,
    double X,
    double Y,
    double Width,
    double Height,
    double Rotation,
    int ZIndex,
    double Opacity,
    bool Locked,
    bool Visible,
    object Config,
    /// <summary>Null = not template-locked. Non-null = the whitelisted config keys; see Element.EditableProperties.</summary>
    List<string>? EditableProperties)
{
    public static ElementDto FromEntity(Element element) => new(
        element.Id,
        element.SceneId,
        element.Type,
        element.Name,
        element.X,
        element.Y,
        element.Width,
        element.Height,
        element.Rotation,
        element.ZIndex,
        element.Opacity,
        element.Locked,
        element.Visible,
        ElementConfigSerializer.Deserialize(element.Type, element.Config),
        element.EditableProperties);
}

public record CreateElementRequest(
    ElementType Type,
    string Name,
    double X,
    double Y,
    double Width,
    double Height,
    JsonElement Config);

/// <summary>Geometry/visibility -- what dragging, resizing, locking, and reordering in the editor touch.</summary>
public record UpdateElementTransformRequest(
    double X,
    double Y,
    double Width,
    double Height,
    double Rotation,
    int ZIndex,
    double Opacity,
    bool Locked,
    bool Visible);

public record UpdateElementConfigRequest(JsonElement Config);
