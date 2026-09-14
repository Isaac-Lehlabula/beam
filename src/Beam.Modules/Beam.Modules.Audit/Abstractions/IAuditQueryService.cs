using Beam.Modules.Audit.Contracts;

namespace Beam.Modules.Audit.Abstractions;

/// <summary>
/// Read side of the audit trail. Deliberately does no authorization of its own -- this module
/// has no notion of organization roles. Callers (e.g. OrganizationService) must check the
/// caller is allowed to see an organization's audit log before calling this.
/// </summary>
public interface IAuditQueryService
{
    Task<List<AuditLogEntryDto>> ListForOrganizationAsync(Guid organizationId, int take, CancellationToken ct = default);
}
