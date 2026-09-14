using Beam.Shared.Abstractions;
using Beam.Shared.Realtime;
using Microsoft.AspNetCore.SignalR;

namespace Beam.Api.Hubs;

public class SignalRRealtimeNotifier(IHubContext<LiveHub> hub) : IRealtimeNotifier
{
    public Task NotifySceneChangedAsync(Guid sceneId, CancellationToken ct = default) =>
        hub.Clients.Group(LiveGroupNames.ForScene(sceneId)).SendAsync("sceneChanged", cancellationToken: ct);

    public Task NotifyProjectLiveStateChangedAsync(Guid projectId, CancellationToken ct = default) =>
        hub.Clients.Group(LiveGroupNames.ForProject(projectId)).SendAsync("sceneChanged", cancellationToken: ct);
}
