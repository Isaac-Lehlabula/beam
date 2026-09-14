namespace Beam.Modules.Scenes.Abstractions;

/// <summary>Public contract other modules (Elements, Overlays, ...) use to resolve and authorize a scene.</summary>
public interface ISceneAccessService
{
    Task<Scene> EnsureAccessibleAsync(Guid sceneId, CancellationToken ct = default);
}
