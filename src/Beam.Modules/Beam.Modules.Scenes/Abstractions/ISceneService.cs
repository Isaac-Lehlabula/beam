using Beam.Modules.Scenes.Contracts;

namespace Beam.Modules.Scenes.Abstractions;

public interface ISceneService
{
    Task<List<SceneDto>> ListForProjectAsync(Guid projectId, CancellationToken ct = default);

    Task<SceneDto> CreateAsync(Guid projectId, CreateSceneRequest request, CancellationToken ct = default);

    Task<SceneDto> RenameAsync(Guid projectId, Guid sceneId, RenameSceneRequest request, CancellationToken ct = default);

    Task<SceneDto> DuplicateAsync(Guid projectId, Guid sceneId, CancellationToken ct = default);

    Task DeleteAsync(Guid projectId, Guid sceneId, CancellationToken ct = default);

    Task ReorderAsync(Guid projectId, ReorderScenesRequest request, CancellationToken ct = default);
}
