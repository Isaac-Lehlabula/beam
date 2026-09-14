using Beam.Shared.Entities;

namespace Beam.Modules.LiveControl;

/// <summary>
/// Durable bookkeeping only (who started/ended a broadcast, and when): the actual live
/// overrides (active scene, element visibility) are ephemeral -- see ILiveStateStore.
/// </summary>
public class LiveSession : EntityBase
{
    public Guid ProjectId { get; set; }

    public DateTimeOffset StartedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? EndedAtUtc { get; set; }
}
