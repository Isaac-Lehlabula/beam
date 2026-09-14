namespace Beam.Modules.Audit.Contracts;

public record AuditLogEntryDto(
    Guid Id,
    DateTimeOffset CreatedAtUtc,
    Guid? ActorUserId,
    string Action,
    string TargetType,
    Guid? TargetId,
    string? MetadataJson)
{
    public static AuditLogEntryDto FromEntity(AuditLogEntry entity) => new(
        entity.Id,
        entity.CreatedAtUtc,
        entity.ActorUserId,
        entity.Action,
        entity.TargetType,
        entity.TargetId,
        entity.MetadataJson);
}
