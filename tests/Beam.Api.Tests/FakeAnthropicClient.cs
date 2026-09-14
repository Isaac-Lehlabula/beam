using Beam.Modules.AiGeneration.Abstractions;

namespace Beam.Api.Tests;

/// <summary>Returns a canned response instead of calling the real Anthropic API -- lets the parsing/validation pipeline be tested without live credentials.</summary>
public class FakeAnthropicClient(string response) : IAnthropicClient
{
    public Task<string> CompleteAsync(string systemPrompt, string userPrompt, CancellationToken ct = default) => Task.FromResult(response);
}
