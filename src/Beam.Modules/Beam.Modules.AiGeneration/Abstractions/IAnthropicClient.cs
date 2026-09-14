namespace Beam.Modules.AiGeneration.Abstractions;

/// <summary>A minimal wrapper over the Anthropic Messages API -- just enough for one-shot, non-streaming text completions.</summary>
public interface IAnthropicClient
{
    Task<string> CompleteAsync(string systemPrompt, string userPrompt, CancellationToken ct = default);
}
