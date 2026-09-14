using Beam.Modules.Overlays.Abstractions;
using Microsoft.AspNetCore.SignalR;

namespace Beam.Api.Hubs;

/// <summary>
/// One hub, two roles (ARCHITECTURE.md "Real-Time Communication Architecture"): overlay clients
/// join a group and only ever receive; nothing here lets a client broadcast directly to
/// another client -- every change reaches the group via the server (IRealtimeNotifier), after
/// the mutation that caused it has already been authorized and persisted. Group naming
/// (Beam.Shared.Realtime.LiveGroupNames) is shared with OverlayService, which is what resolves
/// a client's token to the right group here.
/// </summary>
public class LiveHub(IOverlayService overlayService) : Hub
{
    /// <summary>Called by the public overlay page with its capability token, never a raw scene/project id.</summary>
    public async Task JoinOverlay(string token)
    {
        var groupName = await overlayService.ResolveGroupNameAsync(token, Context.ConnectionAborted);
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
    }
}
