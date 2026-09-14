using Beam.Modules.Audit.Contracts;
using Beam.Modules.Organizations.Contracts;

namespace Beam.Modules.Organizations.Abstractions;

public interface IOrganizationService
{
    Task<List<OrganizationDto>> ListMineAsync(CancellationToken ct = default);

    Task<OrganizationDto> CreateAsync(CreateOrganizationRequest request, CancellationToken ct = default);

    Task<List<MembershipDto>> ListMembersAsync(Guid organizationId, CancellationToken ct = default);

    Task<MembershipDto> AddMemberAsync(Guid organizationId, AddMemberRequest request, CancellationToken ct = default);

    Task<MembershipDto> UpdateMemberRoleAsync(Guid organizationId, Guid userId, UpdateMemberRoleRequest request, CancellationToken ct = default);

    Task RemoveMemberAsync(Guid organizationId, Guid userId, CancellationToken ct = default);

    Task<BrandKitDto> GetBrandKitAsync(Guid organizationId, CancellationToken ct = default);

    Task<BrandKitDto> UpdateBrandKitAsync(Guid organizationId, UpdateBrandKitRequest request, CancellationToken ct = default);

    /// <summary>Admin-only: the org-admin action history recorded via IAuditLogger.</summary>
    Task<List<AuditLogEntryDto>> ListAuditLogAsync(Guid organizationId, int take, CancellationToken ct = default);
}
