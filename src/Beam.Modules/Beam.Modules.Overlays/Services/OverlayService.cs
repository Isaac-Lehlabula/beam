using System.Security.Cryptography;
using Beam.Modules.Elements.Abstractions;
using Beam.Modules.Elements.Contracts;
using Beam.Modules.Overlays.Abstractions;
using Beam.Modules.Overlays.Contracts;
using Beam.Modules.Projects.Abstractions;
using Beam.Modules.Scenes.Abstractions;
using Beam.Shared.Abstractions;
using Beam.Shared.Exceptions;
using Beam.Shared.Realtime;
using Microsoft.EntityFrameworkCore;

namespace Beam.Modules.Overlays.Services;

public class OverlayService(
    IOverlaysDbContext db,
    ISceneAccessService sceneAccess,
    IProjectAccessService projectAccess,
    IScenesDbContext scenesDb,
    IProjectsDbContext projectsDb,
    IElementsDbContext elementsDb,
    ILiveStateStore liveState) : IOverlayService
{
    public Task<OverlayTokenDto> GetOrCreateSceneTokenAsync(Guid sceneId, CancellationToken ct = default) =>
        GetOrCreateTokenAsync(sceneId: sceneId, projectId: null, ct);

    public Task<OverlayTokenDto> RotateSceneTokenAsync(Guid sceneId, CancellationToken ct = default) =>
        RotateTokenAsync(sceneId: sceneId, projectId: null, ct);

    public Task<OverlayTokenDto> GetOrCreateProjectTokenAsync(Guid projectId, CancellationToken ct = default) =>
        GetOrCreateTokenAsync(sceneId: null, projectId: projectId, ct);

    public Task<OverlayTokenDto> RotateProjectTokenAsync(Guid projectId, CancellationToken ct = default) =>
        RotateTokenAsync(sceneId: null, projectId: projectId, ct);

    public async Task<string> ResolveGroupNameAsync(string token, CancellationToken ct = default)
    {
        var record = await FindActiveTokenAsync(token, ct);
        return record.SceneId.HasValue
            ? LiveGroupNames.ForScene(record.SceneId.Value)
            : LiveGroupNames.ForProject(record.ProjectId!.Value);
    }

    public async Task<OverlaySnapshotDto> GetSnapshotAsync(string token, CancellationToken ct = default)
    {
        var record = await FindActiveTokenAsync(token, ct);
        record.LastAccessedAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);

        return record.SceneId.HasValue
            ? await BuildFixedSceneSnapshotAsync(record.SceneId.Value, ct)
            : await BuildProjectLiveSnapshotAsync(record.ProjectId!.Value, ct);
    }

    private async Task<OverlayTokenDto> GetOrCreateTokenAsync(Guid? sceneId, Guid? projectId, CancellationToken ct)
    {
        await EnsureAuthorizedAsync(sceneId, projectId, ct);

        var existing = await db.OverlayTokens.FirstOrDefaultAsync(
            t => t.SceneId == sceneId && t.ProjectId == projectId && t.RevokedAt == null, ct);
        if (existing is not null)
        {
            return ToDto(existing);
        }

        var token = new OverlayToken { SceneId = sceneId, ProjectId = projectId, Token = GenerateToken() };
        db.OverlayTokens.Add(token);
        await db.SaveChangesAsync(ct);
        return ToDto(token);
    }

    private async Task<OverlayTokenDto> RotateTokenAsync(Guid? sceneId, Guid? projectId, CancellationToken ct)
    {
        await EnsureAuthorizedAsync(sceneId, projectId, ct);

        var active = await db.OverlayTokens
            .Where(t => t.SceneId == sceneId && t.ProjectId == projectId && t.RevokedAt == null)
            .ToListAsync(ct);
        foreach (var token in active)
        {
            token.RevokedAt = DateTimeOffset.UtcNow;
        }

        var fresh = new OverlayToken { SceneId = sceneId, ProjectId = projectId, Token = GenerateToken() };
        db.OverlayTokens.Add(fresh);
        await db.SaveChangesAsync(ct);
        return ToDto(fresh);
    }

    private async Task EnsureAuthorizedAsync(Guid? sceneId, Guid? projectId, CancellationToken ct)
    {
        if (sceneId.HasValue)
        {
            await sceneAccess.EnsureAccessibleAsync(sceneId.Value, ct);
        }
        else
        {
            await projectAccess.EnsureAccessibleAsync(projectId!.Value, ct);
        }
    }

    private async Task<OverlaySnapshotDto> BuildFixedSceneSnapshotAsync(Guid sceneId, CancellationToken ct)
    {
        // Deliberately bypasses the tenant query filter: this is the intentionally-public,
        // token-guarded read path (mirrors AssetService.GetFileAsync). An anonymous caller has
        // no ICurrentUserContext.UserId, so without IgnoreQueryFilters() this would just 404
        // even for a valid token.
        var scene = await scenesDb.Scenes.IgnoreQueryFilters().FirstOrDefaultAsync(s => s.Id == sceneId, ct)
            ?? throw new NotFoundException("Scene", sceneId);
        var project = await projectsDb.Projects.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.Id == scene.ProjectId, ct)
            ?? throw new NotFoundException("StreamProject", scene.ProjectId);
        var elements = await LoadElementsAsync(sceneId, ct);

        return new OverlaySnapshotDto(scene.Id, project.ResolutionWidth, project.ResolutionHeight, elements);
    }

    private async Task<OverlaySnapshotDto> BuildProjectLiveSnapshotAsync(Guid projectId, CancellationToken ct)
    {
        var project = await projectsDb.Projects.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.Id == projectId, ct)
            ?? throw new NotFoundException("StreamProject", projectId);

        var state = liveState.GetOrCreate(projectId);
        var sceneId = state.CurrentSceneId ?? await scenesDb.Scenes.IgnoreQueryFilters()
            .Where(s => s.ProjectId == projectId)
            .OrderBy(s => s.OrderIndex)
            .Select(s => (Guid?)s.Id)
            .FirstOrDefaultAsync(ct);

        if (sceneId is null)
        {
            return new OverlaySnapshotDto(projectId, project.ResolutionWidth, project.ResolutionHeight, []);
        }

        var elements = await LoadElementsAsync(sceneId.Value, ct);

        // Live overrides win over the saved Visible flag, but never touch the persisted design
        // (ARCHITECTURE.md "Design state vs. live state") -- this merge happens only in the
        // response DTO.
        var merged = elements.Select(e => state.ElementVisibilityOverrides.TryGetValue(e.Id, out var visible)
            ? e with { Visible = visible }
            : e).ToList();

        return new OverlaySnapshotDto(sceneId.Value, project.ResolutionWidth, project.ResolutionHeight, merged);
    }

    private async Task<List<ElementDto>> LoadElementsAsync(Guid sceneId, CancellationToken ct)
    {
        var elements = await elementsDb.Elements.IgnoreQueryFilters()
            .Where(e => e.SceneId == sceneId)
            .OrderBy(e => e.ZIndex)
            .ToListAsync(ct);
        return elements.Select(ElementDto.FromEntity).ToList();
    }

    private async Task<OverlayToken> FindActiveTokenAsync(string token, CancellationToken ct)
    {
        var record = await db.OverlayTokens.FirstOrDefaultAsync(t => t.Token == token && t.RevokedAt == null, ct);
        if (record is null)
        {
            throw new NotFoundException(nameof(OverlayToken), token);
        }

        return record;
    }

    private static string GenerateToken() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');

    private static OverlayTokenDto ToDto(OverlayToken token) => new(token.Token, $"/overlay/{token.Token}");
}
