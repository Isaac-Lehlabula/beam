namespace Beam.Modules.Templates.Contracts;

public record TemplateDto(Guid Id, string Name, TemplateCategory Category, TemplateScope Scope)
{
    public static TemplateDto FromEntity(Template template) => new(template.Id, template.Name, template.Category, template.Scope);
}

public record InstantiateTemplateRequest(string ProjectName, int ResolutionWidth, int ResolutionHeight);

/// <summary>An element to lock when authoring an org template from an existing project's design.</summary>
public record LockedElementSpec(Guid ElementId, List<string> EditableProperties);

public record CreateOrganizationTemplateRequest(
    Guid ProjectId,
    Guid OrganizationId,
    string Name,
    TemplateCategory Category,
    List<LockedElementSpec> LockedElements);
