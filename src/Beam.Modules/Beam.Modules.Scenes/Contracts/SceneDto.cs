namespace Beam.Modules.Scenes.Contracts;

public record SceneDto(Guid Id, Guid ProjectId, string Name, int OrderIndex)
{
    public static SceneDto FromEntity(Scene scene) => new(scene.Id, scene.ProjectId, scene.Name, scene.OrderIndex);
}

public record CreateSceneRequest(string Name);

public record RenameSceneRequest(string Name);

public record ReorderScenesRequest(List<Guid> OrderedSceneIds);
