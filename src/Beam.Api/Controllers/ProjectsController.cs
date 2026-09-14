using Beam.Modules.Projects.Abstractions;
using Beam.Modules.Projects.Contracts;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Beam.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/projects")]
public class ProjectsController(
    IProjectService projectService,
    IValidator<CreateProjectRequest> createValidator,
    IValidator<UpdateProjectRequest> updateValidator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<ProjectDto>>> List(CancellationToken ct) =>
        Ok(await projectService.ListAccessibleAsync(ct));

    [HttpGet("{projectId:guid}")]
    public async Task<ActionResult<ProjectDto>> Get(Guid projectId, CancellationToken ct) =>
        Ok(await projectService.GetAsync(projectId, ct));

    [HttpPost]
    public async Task<ActionResult<ProjectDto>> Create(CreateProjectRequest request, CancellationToken ct)
    {
        await createValidator.ValidateAndThrowAsync(request, ct);
        var project = await projectService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { projectId = project.Id }, project);
    }

    [HttpPatch("{projectId:guid}")]
    public async Task<ActionResult<ProjectDto>> Rename(Guid projectId, UpdateProjectRequest request, CancellationToken ct)
    {
        await updateValidator.ValidateAndThrowAsync(request, ct);
        return Ok(await projectService.RenameAsync(projectId, request, ct));
    }

    [HttpDelete("{projectId:guid}")]
    public async Task<IActionResult> Delete(Guid projectId, CancellationToken ct)
    {
        await projectService.DeleteAsync(projectId, ct);
        return NoContent();
    }
}
