using Beam.Modules.Overlays.Abstractions;
using Beam.Modules.Overlays.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Beam.Api.Controllers;

/// <summary>Authenticated management of a scene's (fixed, single-scene) overlay capability URL.</summary>
[ApiController]
[Authorize]
[Route("api/scenes/{sceneId:guid}/overlay-token")]
public class OverlayTokensController(IOverlayService overlayService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<OverlayTokenDto>> GetOrCreate(Guid sceneId, CancellationToken ct) =>
        Ok(await overlayService.GetOrCreateSceneTokenAsync(sceneId, ct));

    [HttpPost("rotate")]
    public async Task<ActionResult<OverlayTokenDto>> Rotate(Guid sceneId, CancellationToken ct) =>
        Ok(await overlayService.RotateSceneTokenAsync(sceneId, ct));
}

/// <summary>
/// Authenticated management of a project's persistent "follows the live session" overlay
/// capability URL -- see OverlayToken's doc comment for how this differs from the scene-scoped one.
/// </summary>
[ApiController]
[Authorize]
[Route("api/projects/{projectId:guid}/overlay-token")]
public class ProjectOverlayTokensController(IOverlayService overlayService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<OverlayTokenDto>> GetOrCreate(Guid projectId, CancellationToken ct) =>
        Ok(await overlayService.GetOrCreateProjectTokenAsync(projectId, ct));

    [HttpPost("rotate")]
    public async Task<ActionResult<OverlayTokenDto>> Rotate(Guid projectId, CancellationToken ct) =>
        Ok(await overlayService.RotateProjectTokenAsync(projectId, ct));
}
