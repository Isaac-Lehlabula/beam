namespace Beam.Shared.Abstractions;

/// <summary>
/// Lets domain modules (Elements, ...) announce "this scene's design changed" without taking a
/// compile-time dependency on SignalR -- that's an Api-layer concern (see Beam.Api/Hubs/LiveHub).
/// The overlay MVP deliberately broadcasts a single coarse signal rather than granular
/// per-property diffs: the receiving client always does a full REST re-fetch in response (see
/// ARCHITECTURE.md "Overlay Rendering Architecture" -- reload-safety requires this anyway, so
/// there's no correctness gained by also wiring fine-grained diff events for the MVP).
/// </summary>
public interface IRealtimeNotifier
{
    Task NotifySceneChangedAsync(Guid sceneId, CancellationToken ct = default);

    /// <summary>
    /// For project-scoped overlay tokens (Phase 6): fired whenever the live session's active
    /// scene or an element visibility override changes, so a connected overlay re-fetches its
    /// (design + live-override) snapshot. See Overlays' "project overlay" mode.
    /// </summary>
    Task NotifyProjectLiveStateChangedAsync(Guid projectId, CancellationToken ct = default);
}
