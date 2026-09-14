using Beam.Shared.Entities;

namespace Beam.Modules.Organizations;

/// <summary>
/// Deliberately holds only a raw <see cref="UserId"/> (no navigation to BeamUser) so this
/// module doesn't need a compile-time dependency on the Identity module's entity type.
/// </summary>
public class OrganizationMembership : EntityBase
{
    public Guid OrganizationId { get; set; }

    public Organization? Organization { get; set; }

    public Guid UserId { get; set; }

    public OrganizationRole Role { get; set; }
}
