using Beam.Modules.Organizations.Abstractions;
using Beam.Modules.Organizations.Contracts;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Beam.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/organizations")]
public class OrganizationsController(
    IOrganizationService organizationService,
    IValidator<CreateOrganizationRequest> createValidator,
    IValidator<AddMemberRequest> addMemberValidator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<OrganizationDto>>> ListMine(CancellationToken ct) =>
        Ok(await organizationService.ListMineAsync(ct));

    [HttpPost]
    public async Task<ActionResult<OrganizationDto>> Create(CreateOrganizationRequest request, CancellationToken ct)
    {
        await createValidator.ValidateAndThrowAsync(request, ct);
        return Ok(await organizationService.CreateAsync(request, ct));
    }

    [HttpGet("{organizationId:guid}/members")]
    public async Task<ActionResult<List<MembershipDto>>> ListMembers(Guid organizationId, CancellationToken ct) =>
        Ok(await organizationService.ListMembersAsync(organizationId, ct));

    [HttpPost("{organizationId:guid}/members")]
    public async Task<ActionResult<MembershipDto>> AddMember(Guid organizationId, AddMemberRequest request, CancellationToken ct)
    {
        await addMemberValidator.ValidateAndThrowAsync(request, ct);
        return Ok(await organizationService.AddMemberAsync(organizationId, request, ct));
    }

    [HttpPatch("{organizationId:guid}/members/{userId:guid}")]
    public async Task<ActionResult<MembershipDto>> UpdateMemberRole(Guid organizationId, Guid userId, UpdateMemberRoleRequest request, CancellationToken ct) =>
        Ok(await organizationService.UpdateMemberRoleAsync(organizationId, userId, request, ct));

    [HttpDelete("{organizationId:guid}/members/{userId:guid}")]
    public async Task<IActionResult> RemoveMember(Guid organizationId, Guid userId, CancellationToken ct)
    {
        await organizationService.RemoveMemberAsync(organizationId, userId, ct);
        return NoContent();
    }

    [HttpGet("{organizationId:guid}/brand-kit")]
    public async Task<ActionResult<BrandKitDto>> GetBrandKit(Guid organizationId, CancellationToken ct) =>
        Ok(await organizationService.GetBrandKitAsync(organizationId, ct));

    [HttpPut("{organizationId:guid}/brand-kit")]
    public async Task<ActionResult<BrandKitDto>> UpdateBrandKit(Guid organizationId, UpdateBrandKitRequest request, CancellationToken ct) =>
        Ok(await organizationService.UpdateBrandKitAsync(organizationId, request, ct));

    /// <summary>Admin-only. Most recent org-admin/live-control actions recorded against this organization.</summary>
    [HttpGet("{organizationId:guid}/audit-log")]
    public async Task<ActionResult<List<Beam.Modules.Audit.Contracts.AuditLogEntryDto>>> GetAuditLog(
        Guid organizationId, [FromQuery] int take, CancellationToken ct) =>
        Ok(await organizationService.ListAuditLogAsync(organizationId, take is > 0 and <= 200 ? take : 50, ct));
}
