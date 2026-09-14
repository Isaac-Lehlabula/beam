using Beam.Modules.AiGeneration.Abstractions;
using Beam.Modules.AiGeneration.Contracts;
using Beam.Modules.Projects.Contracts;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Beam.Api.Controllers;

/// <summary>Requires AiGeneration:AnthropicApiKey configured -- see AiGenerationOptions's doc comment.</summary>
[ApiController]
[Authorize]
[Route("api/ai")]
public class AiGenerationController(IAiProjectGenerationService generationService, IValidator<GenerateProjectRequest> validator) : ControllerBase
{
    [HttpPost("generate-project")]
    public async Task<ActionResult<ProjectDto>> GenerateProject(GenerateProjectRequest request, CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(request, ct);
        return Ok(await generationService.GenerateProjectAsync(request, ct));
    }
}
