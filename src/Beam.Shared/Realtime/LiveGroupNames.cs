namespace Beam.Shared.Realtime;

/// <summary>
/// The SignalR group-naming scheme, shared between Beam.Api/Hubs/LiveHub (which joins clients
/// to groups) and Beam.Modules.Overlays (which resolves a token to the group it should join) --
/// neither module should hardcode the string format independently.
/// </summary>
public static class LiveGroupNames
{
    public static string ForScene(Guid sceneId) => $"scene:{sceneId}";

    public static string ForProject(Guid projectId) => $"project:{projectId}";
}
