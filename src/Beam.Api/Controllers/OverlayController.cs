using Beam.Modules.Overlays.Abstractions;
using Beam.Modules.Overlays.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Beam.Api.Controllers;

/// <summary>
/// Public: this is the endpoint OBS's embedded browser (or any Browser Source host) calls.
/// No auth is possible here (see ARCHITECTURE.md §3) -- the token itself is the credential.
/// CORS is opened for this endpoint specifically because overlay/control pages connect to the
/// API directly rather than through the authenticated app's same-origin proxy. Rate-limited
/// tighter than the global default (see Program.cs's "PublicToken" policy) since a valid
/// token is the only thing standing between an anonymous caller and this data.
/// </summary>
[ApiController]
[AllowAnonymous]
[EnableCors("PublicOverlay")]
[EnableRateLimiting("PublicToken")]
[Route("api/overlay/{token}")]
public class OverlayController(IOverlayService overlayService) : ControllerBase
{
    [HttpGet("state")]
    public async Task<ActionResult<OverlaySnapshotDto>> GetState(string token, CancellationToken ct) =>
        Ok(await overlayService.GetSnapshotAsync(token, ct));
}
