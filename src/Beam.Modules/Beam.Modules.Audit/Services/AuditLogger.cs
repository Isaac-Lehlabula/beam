using System.Text.Json;
using Beam.Modules.Audit.Abstractions;
using Beam.Modules.Audit.Contracts;
using Beam.Shared.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Beam.Modules.Audit.Services;

public class AuditLogger(IAuditDbContext db, ICurrentUserContext currentUser) : IAuditLogger, IAuditQueryService
{
    public async Task LogAsync(
        string action,
        string targetType,
        Guid? targetId = null,
        Guid? organizationId = null,
        object? metadata = null,
        CancellationToken ct = default)
    {
        db.AuditLogEntries.Add(new AuditLogEntry
        {
            ActorUserId = currentUser.UserId,
            OrganizationId = organizationId,
            Action = action,
            TargetType = targetType,
            TargetId = targetId,
            MetadataJson = metadata is null ? null : JsonSerializer.Serialize(metadata),
        });

        // Saved immediately rather than riding along with the caller's own SaveChangesAsync --
        // several call sites (e.g. LiveControlService's in-memory-only actions) never call
        // SaveChangesAsync themselves, so the audit write needs its own.
        await db.SaveChangesAsync(ct);
    }

    public async Task<List<AuditLogEntryDto>> ListForOrganizationAsync(Guid organizationId, int take, CancellationToken ct = default)
    {
        var entries = await db.AuditLogEntries
            .Where(a => a.OrganizationId == organizationId)
            .OrderByDescending(a => a.CreatedAtUtc)
            .Take(take)
            .ToListAsync(ct);

        return entries.Select(AuditLogEntryDto.FromEntity).ToList();
    }
}
