namespace Beam.Modules.AiGeneration.Contracts;

public record GenerateProjectRequest(string Prompt, int ResolutionWidth = 1920, int ResolutionHeight = 1080);
