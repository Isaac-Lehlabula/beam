using Beam.Modules.Elements.Abstractions;
using Beam.Modules.Elements.Contracts;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Beam.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/scenes/{sceneId:guid}/elements")]
public class ElementsController(
    IElementService elementService,
    IValidator<CreateElementRequest> createValidator,
    IValidator<UpdateElementTransformRequest> transformValidator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<ElementDto>>> List(Guid sceneId, CancellationToken ct) =>
        Ok(await elementService.ListForSceneAsync(sceneId, ct));

    [HttpPost]
    public async Task<ActionResult<ElementDto>> Create(Guid sceneId, CreateElementRequest request, CancellationToken ct)
    {
        await createValidator.ValidateAndThrowAsync(request, ct);
        var element = await elementService.CreateAsync(sceneId, request, ct);
        return CreatedAtAction(nameof(List), new { sceneId }, element);
    }

    [HttpPut("{elementId:guid}/transform")]
    public async Task<ActionResult<ElementDto>> UpdateTransform(Guid sceneId, Guid elementId, UpdateElementTransformRequest request, CancellationToken ct)
    {
        await transformValidator.ValidateAndThrowAsync(request, ct);
        return Ok(await elementService.UpdateTransformAsync(sceneId, elementId, request, ct));
    }

    [HttpPut("{elementId:guid}/config")]
    public async Task<ActionResult<ElementDto>> UpdateConfig(Guid sceneId, Guid elementId, UpdateElementConfigRequest request, CancellationToken ct) =>
        Ok(await elementService.UpdateConfigAsync(sceneId, elementId, request.Config, ct));

    [HttpPost("{elementId:guid}/duplicate")]
    public async Task<ActionResult<ElementDto>> Duplicate(Guid sceneId, Guid elementId, CancellationToken ct) =>
        Ok(await elementService.DuplicateAsync(sceneId, elementId, ct));

    [HttpDelete("{elementId:guid}")]
    public async Task<IActionResult> Delete(Guid sceneId, Guid elementId, CancellationToken ct)
    {
        await elementService.DeleteAsync(sceneId, elementId, ct);
        return NoContent();
    }
}
