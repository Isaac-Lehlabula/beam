using Beam.Modules.LiveControl.Contracts;

namespace Beam.Modules.LiveControl.Abstractions;

public interface ILiveControlService
{
    Task<LiveSessionDto> GetCurrentAsync(Guid projectId, CancellationToken ct = default);

    Task<LiveSessionDto> StartSessionAsync(Guid projectId, CancellationToken ct = default);

    Task<LiveSessionDto> EndSessionAsync(Guid projectId, CancellationToken ct = default);

    Task<LiveSessionDto> SwitchSceneAsync(Guid projectId, Guid sceneId, CancellationToken ct = default);

    Task<LiveSessionDto> SetElementVisibilityAsync(Guid projectId, Guid elementId, bool visible, CancellationToken ct = default);
}
