using Beam.Modules.LiveControl.Abstractions;
using Beam.Modules.LiveControl.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Beam.Api.Controllers;

/// <summary>
/// Authenticated (not a public token-scoped page -- see ARCHITECTURE.md addendum in Phase 6):
/// a producer/streamer operates this from a normal logged-in browser tab, mobile or desktop.
/// Unlike OBS's embedded browser source, a phone browser can complete a real login, so there's
/// no need for a separate capability-token control channel the way overlays require one.
/// </summary>
[ApiController]
[Authorize]
[Route("api/projects/{projectId:guid}/live-session")]
public class LiveControlController(ILiveControlService liveControl) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<LiveSessionDto>> Get(Guid projectId, CancellationToken ct) =>
        Ok(await liveControl.GetCurrentAsync(projectId, ct));

    [HttpPost]
    public async Task<ActionResult<LiveSessionDto>> Start(Guid projectId, CancellationToken ct) =>
        Ok(await liveControl.StartSessionAsync(projectId, ct));

    [HttpDelete]
    public async Task<ActionResult<LiveSessionDto>> End(Guid projectId, CancellationToken ct) =>
        Ok(await liveControl.EndSessionAsync(projectId, ct));

    [HttpPost("switch-scene")]
    public async Task<ActionResult<LiveSessionDto>> SwitchScene(Guid projectId, SwitchSceneRequest request, CancellationToken ct) =>
        Ok(await liveControl.SwitchSceneAsync(projectId, request.SceneId, ct));

    [HttpPost("elements/{elementId:guid}/visibility")]
    public async Task<ActionResult<LiveSessionDto>> SetElementVisibility(
        Guid projectId, Guid elementId, SetElementVisibilityRequest request, CancellationToken ct) =>
        Ok(await liveControl.SetElementVisibilityAsync(projectId, elementId, request.Visible, ct));
}
