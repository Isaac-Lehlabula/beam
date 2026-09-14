using System.Security.Claims;
using Beam.Shared.Abstractions;

namespace Beam.Api.Auth;

public class HttpContextCurrentUserContext(IHttpContextAccessor httpContextAccessor) : ICurrentUserContext
{
    private ClaimsPrincipal? Principal => httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated ?? false;

    public Guid? UserId
    {
        get
        {
            var value = Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(value, out var id) ? id : null;
        }
    }

    // Populated from an "org" claim per membership once org membership (Phase 7) issues them
    // at sign-in; empty for every user today since no membership UI exists yet.
    public IReadOnlyCollection<Guid> OrganizationIds =>
        Principal?.FindAll("org").Select(c => Guid.TryParse(c.Value, out var id) ? id : (Guid?)null)
            .Where(id => id.HasValue).Select(id => id!.Value).ToArray() ?? Array.Empty<Guid>();
}
