namespace Beam.Modules.LiveControl.Contracts;

public record LiveSessionDto(
    Guid ProjectId,
    bool IsActive,
    Guid? CurrentSceneId,
    Dictionary<Guid, bool> ElementVisibilityOverrides);

public record SwitchSceneRequest(Guid SceneId);

public record SetElementVisibilityRequest(bool Visible);
