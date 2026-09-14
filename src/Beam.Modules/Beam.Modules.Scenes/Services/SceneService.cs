using Beam.Modules.Projects.Abstractions;
using Beam.Modules.Scenes.Abstractions;
using Beam.Modules.Scenes.Contracts;
using Beam.Shared.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Beam.Modules.Scenes.Services;

public class SceneService(IScenesDbContext db, IProjectAccessService projectAccess) : ISceneService, ISceneAccessService
{
    public async Task<Scene> EnsureAccessibleAsync(Guid sceneId, CancellationToken ct = default)
    {
        // Relies on Scene's own query filter (BeamDbContext), not a projectId the caller may
        // not have -- this is the entry point other modules (Elements, Overlays) use.
        var scene = await db.Scenes.FirstOrDefaultAsync(s => s.Id == sceneId, ct);
        if (scene is null)
        {
            throw new NotFoundException(nameof(Scene), sceneId);
        }

        return scene;
    }

    public async Task<List<SceneDto>> ListForProjectAsync(Guid projectId, CancellationToken ct = default)
    {
        await projectAccess.EnsureAccessibleAsync(projectId, ct);
        return await db.Scenes
            .Where(s => s.ProjectId == projectId)
            .OrderBy(s => s.OrderIndex)
            .Select(s => SceneDto.FromEntity(s))
            .ToListAsync(ct);
    }

    public async Task<SceneDto> CreateAsync(Guid projectId, CreateSceneRequest request, CancellationToken ct = default)
    {
        await projectAccess.EnsureAccessibleAsync(projectId, ct);
        var nextOrder = await GetNextOrderIndexAsync(projectId, ct);

        var scene = new Scene { ProjectId = projectId, Name = request.Name, OrderIndex = nextOrder };
        db.Scenes.Add(scene);
        await db.SaveChangesAsync(ct);
        return SceneDto.FromEntity(scene);
    }

    public async Task<SceneDto> RenameAsync(Guid projectId, Guid sceneId, RenameSceneRequest request, CancellationToken ct = default)
    {
        var scene = await GetOwnedSceneAsync(projectId, sceneId, ct);
        scene.Name = request.Name;
        scene.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return SceneDto.FromEntity(scene);
    }

    public async Task<SceneDto> DuplicateAsync(Guid projectId, Guid sceneId, CancellationToken ct = default)
    {
        var source = await GetOwnedSceneAsync(projectId, sceneId, ct);
        var nextOrder = await GetNextOrderIndexAsync(projectId, ct);

        var copy = new Scene { ProjectId = projectId, Name = $"{source.Name} copy", OrderIndex = nextOrder };
        db.Scenes.Add(copy);
        await db.SaveChangesAsync(ct);
        return SceneDto.FromEntity(copy);
    }

    public async Task DeleteAsync(Guid projectId, Guid sceneId, CancellationToken ct = default)
    {
        var scene = await GetOwnedSceneAsync(projectId, sceneId, ct);
        db.Scenes.Remove(scene);
        await db.SaveChangesAsync(ct);
    }

    public async Task ReorderAsync(Guid projectId, ReorderScenesRequest request, CancellationToken ct = default)
    {
        await projectAccess.EnsureAccessibleAsync(projectId, ct);
        var scenes = await db.Scenes.Where(s => s.ProjectId == projectId).ToDictionaryAsync(s => s.Id, ct);

        for (var i = 0; i < request.OrderedSceneIds.Count; i++)
        {
            if (scenes.TryGetValue(request.OrderedSceneIds[i], out var scene))
            {
                scene.OrderIndex = i;
                scene.UpdatedAtUtc = DateTimeOffset.UtcNow;
            }
        }

        await db.SaveChangesAsync(ct);
    }

    private async Task<int> GetNextOrderIndexAsync(Guid projectId, CancellationToken ct)
    {
        var maxOrder = await db.Scenes.Where(s => s.ProjectId == projectId).Select(s => (int?)s.OrderIndex).MaxAsync(ct);
        return (maxOrder ?? -1) + 1;
    }

    private async Task<Scene> GetOwnedSceneAsync(Guid projectId, Guid sceneId, CancellationToken ct)
    {
        await projectAccess.EnsureAccessibleAsync(projectId, ct);
        var scene = await db.Scenes.FirstOrDefaultAsync(s => s.Id == sceneId && s.ProjectId == projectId, ct);
        if (scene is null)
        {
            throw new NotFoundException(nameof(Scene), sceneId);
        }

        return scene;
    }
}
