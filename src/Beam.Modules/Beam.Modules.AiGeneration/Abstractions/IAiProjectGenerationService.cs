using Beam.Modules.AiGeneration.Contracts;
using Beam.Modules.Projects.Contracts;

namespace Beam.Modules.AiGeneration.Abstractions;

public interface IAiProjectGenerationService
{
    Task<ProjectDto> GenerateProjectAsync(GenerateProjectRequest request, CancellationToken ct = default);
}
