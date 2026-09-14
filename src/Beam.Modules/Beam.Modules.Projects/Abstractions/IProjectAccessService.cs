namespace Beam.Modules.Projects.Abstractions;

/// <summary>
/// Public contract other modules (Scenes, Elements, Overlays, ...) use to resolve and
/// authorize a project without depending on this module's persistence details.
/// </summary>
public interface IProjectAccessService
{
    /// <summary>
    /// Loads the project, relying on the tenant-scoped EF Core query filter to make a
    /// project belonging to another user/organization indistinguishable from a missing one.
    /// Throws <see cref="Beam.Shared.Exceptions.NotFoundException"/> if inaccessible.
    /// </summary>
    Task<StreamProject> EnsureAccessibleAsync(Guid projectId, CancellationToken ct = default);
}
