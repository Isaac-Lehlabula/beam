using Beam.Modules.Elements.Contracts;

namespace Beam.Modules.Overlays.Contracts;

/// <summary>What the editor shows when a user asks for a scene's overlay URL.</summary>
public record OverlayTokenDto(string Token, string OverlayPath);

/// <summary>
/// Everything the public overlay page needs to render, in one response -- the full-resync
/// payload it fetches on load and on every reconnect (ARCHITECTURE.md "Overlay Rendering Architecture").
/// </summary>
public record OverlaySnapshotDto(Guid SceneId, int ResolutionWidth, int ResolutionHeight, List<ElementDto> Elements);
