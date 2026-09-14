using Beam.Modules.Overlays.Contracts;

namespace Beam.Modules.Overlays.Abstractions;

public interface IOverlayService
{
    /// <summary>Authenticated: returns the scene's active token, issuing one if none exists yet.</summary>
    Task<OverlayTokenDto> GetOrCreateSceneTokenAsync(Guid sceneId, CancellationToken ct = default);

    /// <summary>Authenticated: revokes the scene's current token and issues a fresh one.</summary>
    Task<OverlayTokenDto> RotateSceneTokenAsync(Guid sceneId, CancellationToken ct = default);

    /// <summary>Authenticated: the project's persistent "follows the live session" overlay token.</summary>
    Task<OverlayTokenDto> GetOrCreateProjectTokenAsync(Guid projectId, CancellationToken ct = default);

    /// <summary>Authenticated: revokes the project's current token and issues a fresh one.</summary>
    Task<OverlayTokenDto> RotateProjectTokenAsync(Guid projectId, CancellationToken ct = default);

    /// <summary>Anonymous: resolves a token to the SignalR group its connection should join.</summary>
    Task<string> ResolveGroupNameAsync(string token, CancellationToken ct = default);

    /// <summary>Anonymous: the full render snapshot for a token (design + any live overrides).</summary>
    Task<OverlaySnapshotDto> GetSnapshotAsync(string token, CancellationToken ct = default);
}
