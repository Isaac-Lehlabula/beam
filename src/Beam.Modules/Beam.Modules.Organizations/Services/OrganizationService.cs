using Beam.Modules.Audit.Abstractions;
using Beam.Modules.Audit.Contracts;
using Beam.Modules.Organizations.Abstractions;
using Beam.Modules.Organizations.Contracts;
using Beam.Shared.Abstractions;
using Beam.Shared.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Beam.Modules.Organizations.Services;

/// <summary>Admin-equivalent roles: can manage members and the brand kit.</summary>
public class OrganizationService(
    IOrganizationsDbContext db,
    ICurrentUserContext currentUser,
    IUserLookupService userLookup,
    IAuditLogger auditLogger,
    IAuditQueryService auditQuery)
    : IOrganizationService, IOrganizationAccessService
{
    private static readonly HashSet<OrganizationRole> AdminRoles = [OrganizationRole.Owner, OrganizationRole.OrgAdmin];

    public async Task<OrganizationRole> EnsureMemberAsync(Guid organizationId, CancellationToken ct = default)
    {
        if (currentUser.UserId is null)
        {
            throw new ForbiddenAccessException("A signed-in user is required.");
        }

        var membership = await db.OrganizationMemberships
            .FirstOrDefaultAsync(m => m.OrganizationId == organizationId && m.UserId == currentUser.UserId, ct);
        if (membership is null)
        {
            throw new NotFoundException(nameof(Organization), organizationId);
        }

        return membership.Role;
    }

    public async Task EnsureRoleAsync(Guid organizationId, IReadOnlySet<OrganizationRole> allowedRoles, CancellationToken ct = default)
    {
        var role = await EnsureMemberAsync(organizationId, ct);
        if (!allowedRoles.Contains(role))
        {
            throw new ForbiddenAccessException($"Role '{role}' is not permitted to perform this action.");
        }
    }

    public async Task<List<OrganizationDto>> ListMineAsync(CancellationToken ct = default)
    {
        if (currentUser.UserId is null)
        {
            return [];
        }

        var myMemberships = await db.OrganizationMemberships
            .Where(m => m.UserId == currentUser.UserId)
            .ToListAsync(ct);
        var orgIds = myMemberships.Select(m => m.OrganizationId).ToList();
        var orgs = await db.Organizations.Where(o => orgIds.Contains(o.Id)).ToListAsync(ct);
        var roleByOrgId = myMemberships.ToDictionary(m => m.OrganizationId, m => m.Role);

        return orgs.Select(o => OrganizationDto.FromEntity(o, roleByOrgId[o.Id])).ToList();
    }

    public async Task<OrganizationDto> CreateAsync(CreateOrganizationRequest request, CancellationToken ct = default)
    {
        if (currentUser.UserId is null)
        {
            throw new ForbiddenAccessException("A signed-in user is required to create an organization.");
        }

        var org = new Organization { Name = request.Name };
        db.Organizations.Add(org);
        db.OrganizationMemberships.Add(new OrganizationMembership
        {
            OrganizationId = org.Id,
            UserId = currentUser.UserId.Value,
            Role = OrganizationRole.Owner,
        });

        await db.SaveChangesAsync(ct);
        return OrganizationDto.FromEntity(org, OrganizationRole.Owner);
    }

    public async Task<List<MembershipDto>> ListMembersAsync(Guid organizationId, CancellationToken ct = default)
    {
        await EnsureMemberAsync(organizationId, ct);

        var memberships = await db.OrganizationMemberships.Where(m => m.OrganizationId == organizationId).ToListAsync(ct);
        var result = new List<MembershipDto>();
        foreach (var membership in memberships)
        {
            var displayName = await userLookup.GetDisplayNameAsync(membership.UserId, ct);
            result.Add(new MembershipDto(membership.UserId, displayName, membership.Role));
        }

        return result;
    }

    public async Task<MembershipDto> AddMemberAsync(Guid organizationId, AddMemberRequest request, CancellationToken ct = default)
    {
        await EnsureRoleAsync(organizationId, AdminRoles, ct);

        var userId = await userLookup.FindUserIdByEmailAsync(request.Email, ct);
        if (userId is null)
        {
            throw new ValidationAppException($"No Beam account found for '{request.Email}'. They need to sign up first.");
        }

        var existing = await db.OrganizationMemberships
            .FirstOrDefaultAsync(m => m.OrganizationId == organizationId && m.UserId == userId, ct);
        if (existing is not null)
        {
            throw new ValidationAppException("This user is already a member.");
        }

        var membership = new OrganizationMembership { OrganizationId = organizationId, UserId = userId.Value, Role = request.Role };
        db.OrganizationMemberships.Add(membership);
        await db.SaveChangesAsync(ct);

        await auditLogger.LogAsync("organization.member.add", "OrganizationMembership", membership.Id, organizationId, new { userId = userId.Value, role = request.Role }, ct);
        return new MembershipDto(userId.Value, await userLookup.GetDisplayNameAsync(userId.Value, ct), request.Role);
    }

    public async Task<MembershipDto> UpdateMemberRoleAsync(Guid organizationId, Guid userId, UpdateMemberRoleRequest request, CancellationToken ct = default)
    {
        await EnsureRoleAsync(organizationId, AdminRoles, ct);

        var membership = await db.OrganizationMemberships
            .FirstOrDefaultAsync(m => m.OrganizationId == organizationId && m.UserId == userId, ct);
        if (membership is null)
        {
            throw new NotFoundException(nameof(OrganizationMembership), userId);
        }

        membership.Role = request.Role;
        await db.SaveChangesAsync(ct);

        await auditLogger.LogAsync("organization.member.role_update", "OrganizationMembership", membership.Id, organizationId, new { userId, role = request.Role }, ct);
        return new MembershipDto(userId, await userLookup.GetDisplayNameAsync(userId, ct), request.Role);
    }

    public async Task RemoveMemberAsync(Guid organizationId, Guid userId, CancellationToken ct = default)
    {
        await EnsureRoleAsync(organizationId, AdminRoles, ct);

        var membership = await db.OrganizationMemberships
            .FirstOrDefaultAsync(m => m.OrganizationId == organizationId && m.UserId == userId, ct);
        if (membership is null)
        {
            throw new NotFoundException(nameof(OrganizationMembership), userId);
        }

        db.OrganizationMemberships.Remove(membership);
        await db.SaveChangesAsync(ct);

        await auditLogger.LogAsync("organization.member.remove", "OrganizationMembership", membership.Id, organizationId, new { userId }, ct);
    }

    public async Task<BrandKitDto> GetBrandKitAsync(Guid organizationId, CancellationToken ct = default)
    {
        await EnsureMemberAsync(organizationId, ct);
        var kit = await GetOrCreateBrandKitAsync(organizationId, ct);
        return BrandKitDto.FromEntity(kit);
    }

    public async Task<BrandKitDto> UpdateBrandKitAsync(Guid organizationId, UpdateBrandKitRequest request, CancellationToken ct = default)
    {
        await EnsureRoleAsync(organizationId, AdminRoles, ct);

        var kit = await GetOrCreateBrandKitAsync(organizationId, ct);
        kit.Colors = request.Colors;
        kit.Fonts = request.Fonts;
        kit.LogoAssetId = request.LogoAssetId;
        kit.WatermarkAssetId = request.WatermarkAssetId;
        kit.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);

        await auditLogger.LogAsync("organization.brand_kit.update", "BrandKit", kit.Id, organizationId, ct: ct);
        return BrandKitDto.FromEntity(kit);
    }

    public async Task<List<AuditLogEntryDto>> ListAuditLogAsync(Guid organizationId, int take, CancellationToken ct = default)
    {
        await EnsureRoleAsync(organizationId, AdminRoles, ct);
        return await auditQuery.ListForOrganizationAsync(organizationId, take, ct);
    }

    private async Task<BrandKit> GetOrCreateBrandKitAsync(Guid organizationId, CancellationToken ct)
    {
        var kit = await db.BrandKits.FirstOrDefaultAsync(k => k.OrganizationId == organizationId, ct);
        if (kit is not null)
        {
            return kit;
        }

        kit = new BrandKit { OrganizationId = organizationId };
        db.BrandKits.Add(kit);
        await db.SaveChangesAsync(ct);
        return kit;
    }
}
