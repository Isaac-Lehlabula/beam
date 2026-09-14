using Beam.Modules.StreamingDestinations;
using Beam.Modules.StreamingDestinations.Abstractions;
using Beam.Modules.StreamingDestinations.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Beam.Api.Controllers;

/// <summary>
/// Requires Integrations:Twitch/YouTube ClientId+ClientSecret configured (each provider needs
/// its own registered OAuth app -- see StreamingIntegrationOptions's doc comment). Every
/// endpoint here throws a clear ValidationAppException (400) rather than failing confusingly
/// if a provider isn't configured.
/// </summary>
[ApiController]
[Authorize]
[Route("api/integrations/streaming")]
public class StreamingDestinationsController(IStreamingDestinationService streamingService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<StreamingConnectionDto>>> ListConnections(CancellationToken ct) =>
        Ok(await streamingService.ListConnectionsAsync(ct));

    [HttpGet("{provider}/authorize-url")]
    public ActionResult<AuthorizationUrlDto> GetAuthorizationUrl(StreamingProvider provider, [FromQuery] string redirectUri, [FromQuery] string state) =>
        Ok(new AuthorizationUrlDto(streamingService.BuildAuthorizationUrl(provider, redirectUri, state)));

    [HttpPost("{provider}/callback")]
    public async Task<ActionResult<StreamingConnectionDto>> HandleCallback(StreamingProvider provider, OAuthCallbackRequest request, CancellationToken ct) =>
        Ok(await streamingService.HandleCallbackAsync(provider, request, ct));

    [HttpGet("{connectionId:guid}/metrics")]
    public async Task<ActionResult<ViewerMetrics>> GetMetrics(Guid connectionId, CancellationToken ct) =>
        Ok(await streamingService.GetViewerMetricsAsync(connectionId, ct));

    [HttpDelete("{connectionId:guid}")]
    public async Task<IActionResult> Disconnect(Guid connectionId, CancellationToken ct)
    {
        await streamingService.DisconnectAsync(connectionId, ct);
        return NoContent();
    }
}
