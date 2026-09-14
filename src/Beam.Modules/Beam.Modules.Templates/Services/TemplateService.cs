using System.Text.Json;
using Beam.Modules.Elements;
using Beam.Modules.Elements.Abstractions;
using Beam.Modules.Organizations;
using Beam.Modules.Organizations.Abstractions;
using Beam.Modules.Projects;
using Beam.Modules.Projects.Abstractions;
using Beam.Modules.Projects.Contracts;
using Beam.Modules.Scenes;
using Beam.Modules.Scenes.Abstractions;
using Beam.Modules.Templates.Abstractions;
using Beam.Modules.Templates.Contracts;
using Beam.Shared.Abstractions;
using Beam.Shared.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Beam.Modules.Templates.Services;

public class TemplateService(
    ITemplatesDbContext templatesDb,
    IProjectsDbContext projectsDb,
    IScenesDbContext scenesDb,
    IElementsDbContext elementsDb,
    IProjectAccessService projectAccess,
    IOrganizationAccessService organizationAccess,
    ICurrentUserContext currentUser) : ITemplateService
{
    private static readonly HashSet<OrganizationRole> TemplateAuthoringRoles =
        [OrganizationRole.Owner, OrganizationRole.OrgAdmin, OrganizationRole.Designer];

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<List<TemplateDto>> ListAvailableAsync(CancellationToken ct = default) =>
        // The System-or-my-org visibility rule is a global query filter on BeamDbContext
        // (same pattern as every other tenant-scoped entity), not repeated here.
        await templatesDb.Templates
            .OrderBy(t => t.Category).ThenBy(t => t.Name)
            .Select(t => TemplateDto.FromEntity(t))
            .ToListAsync(ct);

    public async Task<ProjectDto> InstantiateAsync(Guid templateId, InstantiateTemplateRequest request, CancellationToken ct = default)
    {
        if (currentUser.UserId is null)
        {
            throw new ForbiddenAccessException("A signed-in user is required to create a project.");
        }

        var template = await templatesDb.Templates.FirstOrDefaultAsync(t => t.Id == templateId, ct);
        if (template is null)
        {
            throw new NotFoundException(nameof(Template), templateId);
        }

        var definition = JsonSerializer.Deserialize<TemplateDefinition>(template.DefinitionJson, JsonOptions)
            ?? throw new InvalidOperationException($"Template '{templateId}' has a malformed definition.");

        Guid? organizationId = null;
        if (template.Scope == TemplateScope.Organization)
        {
            // An org template's instantiated project belongs to the org too, so every member
            // can see/edit it -- not just whoever happened to click "use template".
            await organizationAccess.EnsureMemberAsync(template.OrganizationId!.Value, ct);
            organizationId = template.OrganizationId;
        }

        return await InstantiateDefinitionAsync(definition, request.ProjectName, request.ResolutionWidth, request.ResolutionHeight, organizationId, ct);
    }

    /// <summary>
    /// The shared core both regular template instantiation and AI-assisted generation
    /// (Beam.Modules.AiGeneration) go through -- per ARCHITECTURE.md §11, AI generation is
    /// deliberately "produce a Template-shaped payload, then reuse this," not a parallel
    /// rendering/creation path. organizationId null creates a personal project.
    /// </summary>
    public async Task<ProjectDto> InstantiateDefinitionAsync(
        TemplateDefinition definition, string projectName, int resolutionWidth, int resolutionHeight, Guid? organizationId, CancellationToken ct = default)
    {
        if (currentUser.UserId is null)
        {
            throw new ForbiddenAccessException("A signed-in user is required to create a project.");
        }

        var project = organizationId is { } orgId
            ? new StreamProject
            {
                Name = projectName,
                OwnerType = OwnerType.Organization,
                OwnerOrganizationId = orgId,
                ResolutionWidth = resolutionWidth,
                ResolutionHeight = resolutionHeight,
            }
            : new StreamProject
            {
                Name = projectName,
                OwnerType = OwnerType.User,
                OwnerUserId = currentUser.UserId,
                ResolutionWidth = resolutionWidth,
                ResolutionHeight = resolutionHeight,
            };
        projectsDb.Projects.Add(project);

        for (var sceneIndex = 0; sceneIndex < definition.Scenes.Count; sceneIndex++)
        {
            var sceneDefinition = definition.Scenes[sceneIndex];
            var scene = new Scene { ProjectId = project.Id, Name = sceneDefinition.Name, OrderIndex = sceneIndex };
            scenesDb.Scenes.Add(scene);

            for (var elementIndex = 0; elementIndex < sceneDefinition.Elements.Count; elementIndex++)
            {
                var elementDefinition = sceneDefinition.Elements[elementIndex];
                elementsDb.Elements.Add(new Element
                {
                    SceneId = scene.Id,
                    Type = elementDefinition.Type,
                    Name = elementDefinition.Name,
                    X = elementDefinition.X,
                    Y = elementDefinition.Y,
                    Width = elementDefinition.Width,
                    Height = elementDefinition.Height,
                    Rotation = elementDefinition.Rotation,
                    Opacity = elementDefinition.Opacity,
                    ZIndex = elementIndex,
                    Config = elementDefinition.Config.GetRawText(),
                    // Provenance is recorded whenever an element comes from a template; the lock
                    // itself is controlled entirely by EditableProperties being non-null (see
                    // Element.EditableProperties) -- System-template elements (and AI-generated
                    // ones) normally leave it null (fully editable) while Organization-template
                    // elements may not.
                    SourceTemplateElementId = Guid.NewGuid(),
                    EditableProperties = elementDefinition.EditableProperties,
                });
            }
        }

        // One SaveChanges for the whole tree: instantiation is all-or-nothing, and a brand new
        // project has no overlay token yet, so there's nothing to notify in real time.
        await projectsDb.SaveChangesAsync(ct);

        return ProjectDto.FromEntity(project);
    }

    public async Task<TemplateDto> CreateOrganizationTemplateAsync(CreateOrganizationTemplateRequest request, CancellationToken ct = default)
    {
        await organizationAccess.EnsureRoleAsync(request.OrganizationId, TemplateAuthoringRoles, ct);
        await projectAccess.EnsureAccessibleAsync(request.ProjectId, ct);

        var lockedById = request.LockedElements.ToDictionary(l => l.ElementId, l => l.EditableProperties);

        var scenes = await scenesDb.Scenes
            .Where(s => s.ProjectId == request.ProjectId)
            .OrderBy(s => s.OrderIndex)
            .ToListAsync(ct);

        var sceneDefinitions = new List<TemplateSceneDefinition>();
        foreach (var scene in scenes)
        {
            var elements = await elementsDb.Elements
                .Where(e => e.SceneId == scene.Id)
                .OrderBy(e => e.ZIndex)
                .ToListAsync(ct);

            var elementDefinitions = new List<TemplateElementDefinition>();
            foreach (var e in elements)
            {
                using var configDoc = JsonDocument.Parse(e.Config);
                elementDefinitions.Add(new TemplateElementDefinition(
                    e.Type, e.Name, e.X, e.Y, e.Width, e.Height, e.Rotation, e.Opacity,
                    configDoc.RootElement.Clone(),
                    lockedById.GetValueOrDefault(e.Id)));
            }

            sceneDefinitions.Add(new TemplateSceneDefinition(scene.Name, elementDefinitions));
        }

        var template = new Template
        {
            Name = request.Name,
            Category = request.Category,
            Scope = TemplateScope.Organization,
            OrganizationId = request.OrganizationId,
            DefinitionJson = JsonSerializer.Serialize(new TemplateDefinition(sceneDefinitions), JsonOptions),
        };

        templatesDb.Templates.Add(template);
        await templatesDb.SaveChangesAsync(ct);
        return TemplateDto.FromEntity(template);
    }
}
