using Microsoft.EntityFrameworkCore;

namespace Beam.Modules.Overlays.Abstractions;

public interface IOverlaysDbContext
{
    DbSet<OverlayToken> OverlayTokens { get; }

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
