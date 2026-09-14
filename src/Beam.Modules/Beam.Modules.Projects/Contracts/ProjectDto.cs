namespace Beam.Modules.Projects.Contracts;

public record ProjectDto(
    Guid Id,
    string Name,
    int ResolutionWidth,
    int ResolutionHeight,
    Guid? OrganizationId,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc)
{
    public static ProjectDto FromEntity(StreamProject project) => new(
        project.Id,
        project.Name,
        project.ResolutionWidth,
        project.ResolutionHeight,
        project.OwnerOrganizationId,
        project.CreatedAtUtc,
        project.UpdatedAtUtc);
}

/// <summary>OrganizationId null creates a personal project; set it to create one under that org (caller must be a member).</summary>
public record CreateProjectRequest(string Name, int ResolutionWidth, int ResolutionHeight, Guid? OrganizationId = null);

public record UpdateProjectRequest(string Name);

/// <summary>Common presets surfaced by the frontend's resolution picker; not enforced server-side beyond sane bounds.</summary>
public static class StreamResolutionPresets
{
    public static readonly (string Label, int Width, int Height)[] All =
    [
        ("1080p (1920x1080)", 1920, 1080),
        ("720p (1280x720)", 1280, 720),
    ];
}
