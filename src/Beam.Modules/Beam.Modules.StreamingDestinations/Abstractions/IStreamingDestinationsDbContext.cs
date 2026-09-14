using Microsoft.EntityFrameworkCore;

namespace Beam.Modules.StreamingDestinations.Abstractions;

public interface IStreamingDestinationsDbContext
{
    DbSet<StreamingDestinationConnection> StreamingDestinationConnections { get; }

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
