using Microsoft.EntityFrameworkCore;

namespace Beam.Modules.Templates.Abstractions;

public interface ITemplatesDbContext
{
    DbSet<Template> Templates { get; }

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
