using Microsoft.EntityFrameworkCore;

namespace Beam.Modules.LiveControl.Abstractions;

public interface ILiveControlDbContext
{
    DbSet<LiveSession> LiveSessions { get; }

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
