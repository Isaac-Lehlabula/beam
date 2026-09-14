using System.Collections.Concurrent;

namespace Beam.Shared.Abstractions;

/// <summary>
/// Ephemeral (in-memory, not persisted) per-project live overrides -- the "live state" layer
/// from ARCHITECTURE.md "Overlay Rendering Architecture": what a producer toggles during a
/// broadcast, layered on top of the saved design at render time, never written back into it.
/// Single-instance in-memory is intentional for the MVP (ARCHITECTURE.md §14); a Redis-backed
/// implementation is a drop-in swap behind this interface if/when the API scales horizontally.
/// </summary>
public interface ILiveStateStore
{
    LiveState GetOrCreate(Guid projectId);

    void Clear(Guid projectId);
}

public class LiveState
{
    public Guid? CurrentSceneId { get; set; }

    public ConcurrentDictionary<Guid, bool> ElementVisibilityOverrides { get; } = new();
}

public class InMemoryLiveStateStore : ILiveStateStore
{
    private readonly ConcurrentDictionary<Guid, LiveState> _states = new();

    public LiveState GetOrCreate(Guid projectId) => _states.GetOrAdd(projectId, static _ => new LiveState());

    public void Clear(Guid projectId) => _states.TryRemove(projectId, out _);
}
