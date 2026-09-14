using Microsoft.EntityFrameworkCore;

namespace Beam.Modules.Organizations.Abstractions;

/// <summary>
/// The narrow slice of the shared DbContext this module is allowed to see. Implemented by
/// the concrete BeamDbContext in Beam.Infrastructure.
/// </summary>
public interface IOrganizationsDbContext
{
    DbSet<Organization> Organizations { get; }

    DbSet<OrganizationMembership> OrganizationMemberships { get; }

    DbSet<BrandKit> BrandKits { get; }

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
