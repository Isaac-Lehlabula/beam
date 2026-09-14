using Microsoft.EntityFrameworkCore;

namespace Beam.Modules.Scenes.Abstractions;

public interface IScenesDbContext
{
    DbSet<Scene> Scenes { get; }

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
