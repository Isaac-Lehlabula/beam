using Beam.Modules.StreamingDestinations.Contracts;

namespace Beam.Modules.StreamingDestinations.Abstractions;

public interface IStreamingDestinationService
{
    string BuildAuthorizationUrl(StreamingProvider provider, string redirectUri, string state);

    Task<StreamingConnectionDto> HandleCallbackAsync(StreamingProvider provider, OAuthCallbackRequest request, CancellationToken ct = default);

    Task<List<StreamingConnectionDto>> ListConnectionsAsync(CancellationToken ct = default);

    Task<ViewerMetrics> GetViewerMetricsAsync(Guid connectionId, CancellationToken ct = default);

    Task DisconnectAsync(Guid connectionId, CancellationToken ct = default);
}
