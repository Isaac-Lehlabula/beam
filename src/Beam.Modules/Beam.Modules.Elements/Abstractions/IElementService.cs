using System.Text.Json;
using Beam.Modules.Elements.Contracts;

namespace Beam.Modules.Elements.Abstractions;

public interface IElementService
{
    Task<List<ElementDto>> ListForSceneAsync(Guid sceneId, CancellationToken ct = default);

    Task<ElementDto> CreateAsync(Guid sceneId, CreateElementRequest request, CancellationToken ct = default);

    Task<ElementDto> UpdateTransformAsync(Guid sceneId, Guid elementId, UpdateElementTransformRequest request, CancellationToken ct = default);

    Task<ElementDto> UpdateConfigAsync(Guid sceneId, Guid elementId, JsonElement config, CancellationToken ct = default);

    Task<ElementDto> DuplicateAsync(Guid sceneId, Guid elementId, CancellationToken ct = default);

    Task DeleteAsync(Guid sceneId, Guid elementId, CancellationToken ct = default);
}
