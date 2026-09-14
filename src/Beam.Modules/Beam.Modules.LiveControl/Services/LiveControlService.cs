using Beam.Modules.Audit.Abstractions;
using Beam.Modules.LiveControl.Abstractions;
using Beam.Modules.LiveControl.Contracts;
using Beam.Modules.Projects.Abstractions;
using Beam.Modules.Scenes.Abstractions;
using Beam.Shared.Abstractions;
using Beam.Shared.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Beam.Modules.LiveControl.Services;

public class LiveControlService(
    ILiveControlDbContext db,
    ILiveStateStore liveState,
    IProjectAccessService projectAccess,
    ISceneAccessService sceneAccess,
    IRealtimeNotifier realtime,
    IAuditLogger auditLogger) : ILiveControlService
{
    public async Task<LiveSessionDto> GetCurrentAsync(Guid projectId, CancellationToken ct = default)
    {
        await projectAccess.EnsureAccessibleAsync(projectId, ct);
        var session = await ActiveSessionAsync(projectId, ct);
        return ToDto(projectId, session);
    }

    public async Task<LiveSessionDto> StartSessionAsync(Guid projectId, CancellationToken ct = default)
    {
        var project = await projectAccess.EnsureAccessibleAsync(projectId, ct);

        var existing = await ActiveSessionAsync(projectId, ct);
        if (existing is null)
        {
            db.LiveSessions.Add(new LiveSession { ProjectId = projectId });
            await db.SaveChangesAsync(ct);
        }

        await auditLogger.LogAsync("live_session.start", "Project", projectId, project.OwnerOrganizationId, ct: ct);
        await realtime.NotifyProjectLiveStateChangedAsync(projectId, ct);
        return await GetCurrentAsync(projectId, ct);
    }

    public async Task<LiveSessionDto> EndSessionAsync(Guid projectId, CancellationToken ct = default)
    {
        var project = await projectAccess.EnsureAccessibleAsync(projectId, ct);

        var session = await ActiveSessionAsync(projectId, ct);
        if (session is not null)
        {
            session.EndedAtUtc = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(ct);
        }

        liveState.Clear(projectId);
        await auditLogger.LogAsync("live_session.end", "Project", projectId, project.OwnerOrganizationId, ct: ct);
        await realtime.NotifyProjectLiveStateChangedAsync(projectId, ct);
        return ToDto(projectId, null);
    }

    public async Task<LiveSessionDto> SwitchSceneAsync(Guid projectId, Guid sceneId, CancellationToken ct = default)
    {
        var project = await projectAccess.EnsureAccessibleAsync(projectId, ct);
        var scene = await sceneAccess.EnsureAccessibleAsync(sceneId, ct);
        if (scene.ProjectId != projectId)
        {
            throw new NotFoundException("Scene", sceneId);
        }

        liveState.GetOrCreate(projectId).CurrentSceneId = sceneId;
        await auditLogger.LogAsync("live_session.switch_scene", "Project", projectId, project.OwnerOrganizationId, metadata: new { sceneId }, ct: ct);
        await realtime.NotifyProjectLiveStateChangedAsync(projectId, ct);
        return await GetCurrentAsync(projectId, ct);
    }

    public async Task<LiveSessionDto> SetElementVisibilityAsync(Guid projectId, Guid elementId, bool visible, CancellationToken ct = default)
    {
        var project = await projectAccess.EnsureAccessibleAsync(projectId, ct);

        // No element-ownership DB round trip here: an override for an element id that doesn't
        // belong to the current scene is inert by construction -- the overlay's render-time
        // merge only ever looks up overrides for elements it's already about to render.
        liveState.GetOrCreate(projectId).ElementVisibilityOverrides[elementId] = visible;
        await auditLogger.LogAsync("live_session.element_visibility", "Project", projectId, project.OwnerOrganizationId, metadata: new { elementId, visible }, ct: ct);
        await realtime.NotifyProjectLiveStateChangedAsync(projectId, ct);
        return await GetCurrentAsync(projectId, ct);
    }

    private async Task<LiveSession?> ActiveSessionAsync(Guid projectId, CancellationToken ct) =>
        await db.LiveSessions
            .Where(s => s.ProjectId == projectId && s.EndedAtUtc == null)
            .OrderByDescending(s => s.StartedAtUtc)
            .FirstOrDefaultAsync(ct);

    private LiveSessionDto ToDto(Guid projectId, LiveSession? session)
    {
        var state = liveState.GetOrCreate(projectId);
        return new LiveSessionDto(
            projectId,
            session is not null,
            state.CurrentSceneId,
            new Dictionary<Guid, bool>(state.ElementVisibilityOverrides));
    }
}
