using Beam.Shared.Entities;

namespace Beam.Modules.Projects;

public class StreamProject : EntityBase
{
    public string Name { get; set; } = string.Empty;

    public OwnerType OwnerType { get; set; }

    public Guid? OwnerUserId { get; set; }

    public Guid? OwnerOrganizationId { get; set; }

    public int ResolutionWidth { get; set; } = 1920;

    public int ResolutionHeight { get; set; } = 1080;
}
