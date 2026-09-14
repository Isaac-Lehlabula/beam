namespace Beam.Modules.Audit.Abstractions;

/// <summary>Write side of the audit trail. See AuditLogEntry for what gets recorded and why.</summary>
public interface IAuditLogger
{
    Task LogAsync(
        string action,
        string targetType,
        Guid? targetId = null,
        Guid? organizationId = null,
        object? metadata = null,
        CancellationToken ct = default);
}
