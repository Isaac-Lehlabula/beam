using Microsoft.EntityFrameworkCore;

namespace Beam.Modules.Elements.Abstractions;

public interface IElementsDbContext
{
    DbSet<Element> Elements { get; }

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
