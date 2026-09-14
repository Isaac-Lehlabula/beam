using Beam.Shared.Abstractions;

namespace Beam.Api.Tests;

public class FakeRealtimeNotifier : IRealtimeNotifier
{
    public Task NotifySceneChangedAsync(Guid sceneId, CancellationToken ct = default) => Task.CompletedTask;

    public Task NotifyProjectLiveStateChangedAsync(Guid projectId, CancellationToken ct = default) => Task.CompletedTask;
}
