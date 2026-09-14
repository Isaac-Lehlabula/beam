using Microsoft.EntityFrameworkCore;

namespace Beam.Modules.Projects.Abstractions;

public interface IProjectsDbContext
{
    DbSet<StreamProject> Projects { get; }

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
