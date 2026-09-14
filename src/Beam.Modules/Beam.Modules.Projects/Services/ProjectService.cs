using Beam.Modules.Organizations.Abstractions;
using Beam.Modules.Projects.Abstractions;
using Beam.Modules.Projects.Contracts;
using Beam.Shared.Abstractions;
using Beam.Shared.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Beam.Modules.Projects.Services;

public class ProjectService(IProjectsDbContext db, ICurrentUserContext currentUser, IOrganizationAccessService organizationAccess)
    : IProjectService, IProjectAccessService
{
    public async Task<StreamProject> EnsureAccessibleAsync(Guid projectId, CancellationToken ct = default)
    {
        var project = await db.Projects.FirstOrDefaultAsync(p => p.Id == projectId, ct);
        if (project is null)
        {
            throw new NotFoundException(nameof(StreamProject), projectId);
        }

        return project;
    }

    public async Task<List<ProjectDto>> ListAccessibleAsync(CancellationToken ct = default) =>
        await db.Projects
            .OrderByDescending(p => p.UpdatedAtUtc)
            .Select(p => ProjectDto.FromEntity(p))
            .ToListAsync(ct);

    public async Task<ProjectDto> GetAsync(Guid projectId, CancellationToken ct = default) =>
        ProjectDto.FromEntity(await EnsureAccessibleAsync(projectId, ct));

    public async Task<ProjectDto> CreateAsync(CreateProjectRequest request, CancellationToken ct = default)
    {
        if (currentUser.UserId is null)
        {
            throw new ForbiddenAccessException("A signed-in user is required to create a project.");
        }

        StreamProject project;
        if (request.OrganizationId is { } organizationId)
        {
            // Any member can create a project under their org (matches the brief's "employees"
            // scenario); managing the org itself (members, brand kit) is admin-only, enforced
            // separately in OrganizationService.
            await organizationAccess.EnsureMemberAsync(organizationId, ct);
            project = new StreamProject
            {
                Name = request.Name,
                OwnerType = OwnerType.Organization,
                OwnerOrganizationId = organizationId,
                ResolutionWidth = request.ResolutionWidth,
                ResolutionHeight = request.ResolutionHeight,
            };
        }
        else
        {
            project = new StreamProject
            {
                Name = request.Name,
                OwnerType = OwnerType.User,
                OwnerUserId = currentUser.UserId,
                ResolutionWidth = request.ResolutionWidth,
                ResolutionHeight = request.ResolutionHeight,
            };
        }

        db.Projects.Add(project);
        await db.SaveChangesAsync(ct);
        return ProjectDto.FromEntity(project);
    }

    public async Task<ProjectDto> RenameAsync(Guid projectId, UpdateProjectRequest request, CancellationToken ct = default)
    {
        var project = await EnsureAccessibleAsync(projectId, ct);
        project.Name = request.Name;
        project.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return ProjectDto.FromEntity(project);
    }

    public async Task DeleteAsync(Guid projectId, CancellationToken ct = default)
    {
        var project = await EnsureAccessibleAsync(projectId, ct);
        db.Projects.Remove(project);
        await db.SaveChangesAsync(ct);
    }
}
