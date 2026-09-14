namespace Beam.Shared.Abstractions;

/// <summary>
/// Resolves the acting user for the current request. Implemented in the API host (reads
/// the authenticated ClaimsPrincipal) and injected into the EF Core DbContext so tenant-scoped
/// global query filters can reference it.
/// </summary>
public interface ICurrentUserContext
{
    bool IsAuthenticated { get; }

    Guid? UserId { get; }

    /// <summary>
    /// Organizations the current user belongs to, sourced from auth claims. Empty until
    /// organization membership (Phase 7) is wired up.
    /// </summary>
    IReadOnlyCollection<Guid> OrganizationIds { get; }
}
