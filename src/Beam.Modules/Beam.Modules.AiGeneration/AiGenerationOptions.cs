namespace Beam.Modules.AiGeneration;

/// <summary>
/// Requires an Anthropic API key (https://console.anthropic.com/) set via configuration
/// (AiGeneration:AnthropicApiKey or the matching environment variable). Unset by default;
/// AiProjectGenerationController returns a clear error rather than a confusing failure if
/// it's missing.
/// </summary>
public class AiGenerationOptions
{
    public string? AnthropicApiKey { get; set; }

    /// <summary>claude-sonnet-5 by default -- override for a different model without a code change.</summary>
    public string Model { get; set; } = "claude-sonnet-5";
}
