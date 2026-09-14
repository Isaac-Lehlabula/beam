using Microsoft.EntityFrameworkCore;

namespace Beam.Modules.Audit.Abstractions;

public interface IAuditDbContext
{
    DbSet<AuditLogEntry> AuditLogEntries { get; }

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
