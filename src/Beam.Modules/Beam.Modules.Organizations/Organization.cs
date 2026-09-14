using Beam.Shared.Entities;

namespace Beam.Modules.Organizations;

public class Organization : EntityBase
{
    public string Name { get; set; } = string.Empty;
}
