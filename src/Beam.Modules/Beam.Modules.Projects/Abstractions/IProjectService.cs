using Beam.Modules.Projects.Contracts;

namespace Beam.Modules.Projects.Abstractions;

public interface IProjectService
{
    Task<List<ProjectDto>> ListAccessibleAsync(CancellationToken ct = default);

    Task<ProjectDto> GetAsync(Guid projectId, CancellationToken ct = default);

    Task<ProjectDto> CreateAsync(CreateProjectRequest request, CancellationToken ct = default);

    Task<ProjectDto> RenameAsync(Guid projectId, UpdateProjectRequest request, CancellationToken ct = default);

    Task DeleteAsync(Guid projectId, CancellationToken ct = default);
}
