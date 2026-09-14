using Beam.Modules.Projects.Contracts;
using Beam.Modules.Templates.Abstractions;
using Beam.Modules.Templates.Contracts;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Beam.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/templates")]
public class TemplatesController(
    ITemplateService templateService,
    IValidator<InstantiateTemplateRequest> instantiateValidator,
    IValidator<CreateOrganizationTemplateRequest> createOrgTemplateValidator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<TemplateDto>>> List(CancellationToken ct) =>
        Ok(await templateService.ListAvailableAsync(ct));

    [HttpPost("{templateId:guid}/instantiate")]
    public async Task<ActionResult<ProjectDto>> Instantiate(Guid templateId, InstantiateTemplateRequest request, CancellationToken ct)
    {
        await instantiateValidator.ValidateAndThrowAsync(request, ct);
        return Ok(await templateService.InstantiateAsync(templateId, request, ct));
    }

    [HttpPost("from-project")]
    public async Task<ActionResult<TemplateDto>> CreateFromProject(CreateOrganizationTemplateRequest request, CancellationToken ct)
    {
        await createOrgTemplateValidator.ValidateAndThrowAsync(request, ct);
        return Ok(await templateService.CreateOrganizationTemplateAsync(request, ct));
    }
}
