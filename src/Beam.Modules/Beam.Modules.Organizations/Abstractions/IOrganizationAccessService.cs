namespace Beam.Modules.Organizations.Abstractions;

/// <summary>Public contract other modules (Projects, Templates, ...) use to authorize against org membership/role.</summary>
public interface IOrganizationAccessService
{
    /// <summary>Throws NotFoundException if the current user isn't a member of this organization.</summary>
    Task<OrganizationRole> EnsureMemberAsync(Guid organizationId, CancellationToken ct = default);

    /// <summary>Throws NotFoundException if not a member, ForbiddenAccessException if the member's role isn't allowed.</summary>
    Task EnsureRoleAsync(Guid organizationId, IReadOnlySet<OrganizationRole> allowedRoles, CancellationToken ct = default);
}
