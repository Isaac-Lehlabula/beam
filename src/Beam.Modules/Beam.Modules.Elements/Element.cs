using Beam.Modules.Scenes;
using Beam.Shared.Entities;

namespace Beam.Modules.Elements;

/// <summary>
/// Hybrid model per ARCHITECTURE.md §7: transform/common properties are real columns
/// (queried/sorted/constrained generically by the layer panel regardless of element type);
/// <see cref="Config"/> is a jsonb blob whose shape depends on <see cref="Type"/> and is only
/// ever interpreted by the strongly-typed config records in Contracts/.
/// </summary>
public class Element : EntityBase
{
    public Guid SceneId { get; set; }

    /// <summary>Navigation used only for the tenant-isolation query filter (mirrors Scene/StreamProject).</summary>
    public Scene? Scene { get; set; }

    public ElementType Type { get; set; }

    public string Name { get; set; } = string.Empty;

    public double X { get; set; }

    public double Y { get; set; }

    public double Width { get; set; } = 200;

    public double Height { get; set; } = 100;

    public double Rotation { get; set; }

    public int ZIndex { get; set; }

    public double Opacity { get; set; } = 1;

    public bool Locked { get; set; }

    public bool Visible { get; set; } = true;

    /// <summary>Raw JSON; shape is TextElementConfig or ImageElementConfig depending on Type.</summary>
    public string Config { get; set; } = "{}";

    /// <summary>Set when this element was instantiated from an organization template's locked element.</summary>
    public Guid? SourceTemplateElementId { get; set; }

    /// <summary>
    /// Template-lock whitelist (ARCHITECTURE.md §7 "Template locking model"): null means
    /// "not locked, fully editable" (every element outside an org template, and every element
    /// from a System-scope template). A non-null list means position/size/rotation/z-order and
    /// deletion are always forbidden regardless of contents, and only the *config* keys named
    /// here may be changed -- see ElementService's lock enforcement.
    /// </summary>
    public List<string>? EditableProperties { get; set; }
}
