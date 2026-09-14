using Beam.Shared.Entities;

namespace Beam.Modules.Audit;

/// <summary>
/// An append-only record of a security-relevant or state-changing action -- currently written
/// for live-control operations (LiveControlService) and organization-admin operations
/// (OrganizationService). Best-effort, not a compliance-grade WORM log: it shares the acting
/// service's DbContext/connection rather than a separate transactional store, so a write can in
/// theory fail independently of the action it's recording -- acceptable for an internal audit
/// trail, called out explicitly in docs/SECURITY_REVIEW.md.
/// </summary>
public class AuditLogEntry : EntityBase
{
    public Guid? ActorUserId { get; set; }

    /// <summary>Set only when the action relates to an organization-owned resource; null for personal (user-owned) resources.</summary>
    public Guid? OrganizationId { get; set; }

    /// <summary>Dot-separated action name, e.g. "live_session.start", "organization.member.remove".</summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>The kind of entity acted upon, e.g. "Project", "OrganizationMembership".</summary>
    public string TargetType { get; set; } = string.Empty;

    public Guid? TargetId { get; set; }

    /// <summary>Free-form JSON with action-specific detail (e.g. the new role, the scene switched to). Never contains secrets/tokens.</summary>
    public string? MetadataJson { get; set; }
}
