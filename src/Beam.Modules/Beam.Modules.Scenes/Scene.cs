using Beam.Modules.Projects;
using Beam.Shared.Entities;

namespace Beam.Modules.Scenes;

public class Scene : EntityBase
{
    public Guid ProjectId { get; set; }

    /// <summary>
    /// Navigation used only so the query filter (mirroring StreamProject's) can be expressed
    /// declaratively; never eager-loaded for API responses.
    /// </summary>
    public StreamProject? Project { get; set; }

    public string Name { get; set; } = string.Empty;

    public int OrderIndex { get; set; }
}
