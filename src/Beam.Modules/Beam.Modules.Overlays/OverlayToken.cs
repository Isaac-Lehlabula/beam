using Beam.Shared.Entities;

namespace Beam.Modules.Overlays;

/// <summary>
/// The capability credential behind an overlay URL (ARCHITECTURE.md §3/§9): possession of the
/// token is the only access control, since a Browser Source can't complete an interactive login.
///
/// Exactly one of <see cref="SceneId"/>/<see cref="ProjectId"/> is set (enforced by
/// OverlayService, not the database): a scene-scoped token always renders that one scene --
/// the Phase 3 model, for pointing a Browser Source at one specific scene. A project-scoped
/// token renders whichever scene the project's live session currently has active (Phase 6) --
/// for a single persistent Browser Source a producer flips between scenes via Live Control
/// without ever touching OBS.
/// </summary>
public class OverlayToken : EntityBase
{
    public Guid? SceneId { get; set; }

    public Guid? ProjectId { get; set; }

    public string Token { get; set; } = string.Empty;

    public DateTimeOffset? RevokedAt { get; set; }

    public DateTimeOffset? LastAccessedAtUtc { get; set; }

    public bool IsActive => RevokedAt is null;
}
