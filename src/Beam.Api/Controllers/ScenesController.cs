using Beam.Modules.Scenes.Abstractions;
using Beam.Modules.Scenes.Contracts;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Beam.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/projects/{projectId:guid}/scenes")]
public class ScenesController(
    ISceneService sceneService,
    IValidator<CreateSceneRequest> createValidator,
    IValidator<RenameSceneRequest> renameValidator,
    IValidator<ReorderScenesRequest> reorderValidator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<SceneDto>>> List(Guid projectId, CancellationToken ct) =>
        Ok(await sceneService.ListForProjectAsync(projectId, ct));

    [HttpPost]
    public async Task<ActionResult<SceneDto>> Create(Guid projectId, CreateSceneRequest request, CancellationToken ct)
    {
        await createValidator.ValidateAndThrowAsync(request, ct);
        var scene = await sceneService.CreateAsync(projectId, request, ct);
        return CreatedAtAction(nameof(List), new { projectId }, scene);
    }

    [HttpPatch("{sceneId:guid}")]
    public async Task<ActionResult<SceneDto>> Rename(Guid projectId, Guid sceneId, RenameSceneRequest request, CancellationToken ct)
    {
        await renameValidator.ValidateAndThrowAsync(request, ct);
        return Ok(await sceneService.RenameAsync(projectId, sceneId, request, ct));
    }

    [HttpPost("{sceneId:guid}/duplicate")]
    public async Task<ActionResult<SceneDto>> Duplicate(Guid projectId, Guid sceneId, CancellationToken ct) =>
        Ok(await sceneService.DuplicateAsync(projectId, sceneId, ct));

    [HttpDelete("{sceneId:guid}")]
    public async Task<IActionResult> Delete(Guid projectId, Guid sceneId, CancellationToken ct)
    {
        await sceneService.DeleteAsync(projectId, sceneId, ct);
        return NoContent();
    }

    [HttpPost("reorder")]
    public async Task<IActionResult> Reorder(Guid projectId, ReorderScenesRequest request, CancellationToken ct)
    {
        await reorderValidator.ValidateAndThrowAsync(request, ct);
        await sceneService.ReorderAsync(projectId, request, ct);
        return NoContent();
    }
}
