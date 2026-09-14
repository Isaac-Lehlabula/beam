using Beam.Modules.Projects.Contracts;
using Beam.Modules.Templates.Contracts;

namespace Beam.Modules.Templates.Abstractions;

public interface ITemplateService
{
    Task<List<TemplateDto>> ListAvailableAsync(CancellationToken ct = default);

    Task<ProjectDto> InstantiateAsync(Guid templateId, InstantiateTemplateRequest request, CancellationToken ct = default);

    /// <summary>
    /// The shared core other modules (Beam.Modules.AiGeneration) call directly with a
    /// definition they built themselves, rather than one looked up by template id -- see
    /// ARCHITECTURE.md §11.
    /// </summary>
    Task<ProjectDto> InstantiateDefinitionAsync(
        TemplateDefinition definition, string projectName, int resolutionWidth, int resolutionHeight, Guid? organizationId, CancellationToken ct = default);

    /// <summary>
    /// Snapshots an existing (already-designed) project's scenes/elements into a new
    /// organization-scoped template, marking the given elements as locked. Requires
    /// Owner/OrgAdmin/Designer in the target organization.
    /// </summary>
    Task<TemplateDto> CreateOrganizationTemplateAsync(CreateOrganizationTemplateRequest request, CancellationToken ct = default);
}
