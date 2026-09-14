using Microsoft.EntityFrameworkCore;

namespace Beam.Modules.Assets.Abstractions;

public interface IAssetsDbContext
{
    DbSet<Asset> Assets { get; }

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
