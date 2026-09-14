using System.Security.Claims;
using Beam.Modules.Identity;
using Beam.Modules.Organizations.Abstractions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Beam.Api.Auth;

/// <summary>
/// Adds one "org" claim per organization the user belongs to. This is how
/// ICurrentUserContext.OrganizationIds (Beam.Shared) gets populated -- it deliberately reads
/// only from claims, never hits the database itself, to avoid a circular DI dependency with
/// BeamDbContext (see ARCHITECTURE.md/HttpContextCurrentUserContext's original comment).
/// Claims are baked in at sign-in; call POST /api/auth/refresh-claims (or sign out/in) after a
/// membership change to pick up new org claims without waiting for cookie expiry.
/// </summary>
public class BeamUserClaimsPrincipalFactory(
    UserManager<BeamUser> userManager,
    IOptions<IdentityOptions> optionsAccessor,
    IOrganizationsDbContext organizationsDb) : UserClaimsPrincipalFactory<BeamUser>(userManager, optionsAccessor)
{
    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(BeamUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);

        var organizationIds = await organizationsDb.OrganizationMemberships
            .Where(m => m.UserId == user.Id)
            .Select(m => m.OrganizationId)
            .ToListAsync();

        foreach (var organizationId in organizationIds)
        {
            identity.AddClaim(new Claim("org", organizationId.ToString()));
        }

        return identity;
    }
}
