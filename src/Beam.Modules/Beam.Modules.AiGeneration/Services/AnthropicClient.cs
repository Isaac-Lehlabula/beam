using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Beam.Modules.AiGeneration.Abstractions;
using Beam.Shared.Exceptions;
using Microsoft.Extensions.Options;

namespace Beam.Modules.AiGeneration.Services;

/// <summary>
/// Anthropic Messages API (https://docs.claude.com/en/api/messages). One-shot, non-streaming --
/// AI-assisted generation runs once per request, not as a chat, so streaming adds complexity
/// this doesn't need.
/// </summary>
public class AnthropicClient(HttpClient httpClient, IOptions<AiGenerationOptions> options) : IAnthropicClient
{
    private const string MessagesUrl = "https://api.anthropic.com/v1/messages";
    private const string ApiVersion = "2023-06-01";

    public async Task<string> CompleteAsync(string systemPrompt, string userPrompt, CancellationToken ct = default)
    {
        var apiKey = options.Value.AnthropicApiKey;
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new ValidationAppException("AI generation is not configured (missing AiGeneration:AnthropicApiKey).");
        }

        var request = new HttpRequestMessage(HttpMethod.Post, MessagesUrl);
        request.Headers.Add("x-api-key", apiKey);
        request.Headers.Add("anthropic-version", ApiVersion);
        request.Content = JsonContent.Create(new AnthropicRequest(
            options.Value.Model,
            4096,
            systemPrompt,
            [new AnthropicMessage("user", userPrompt)]));

        var response = await httpClient.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new ValidationAppException($"Anthropic API request failed ({(int)response.StatusCode}): {body}");
        }

        var result = await response.Content.ReadFromJsonAsync<AnthropicResponse>(cancellationToken: ct)
            ?? throw new ValidationAppException("Anthropic API returned an empty response.");
        var text = result.Content.FirstOrDefault(c => c.Type == "text")?.Text;
        return text ?? throw new ValidationAppException("Anthropic API response did not contain any text content.");
    }

    private record AnthropicRequest(
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("max_tokens")] int MaxTokens,
        [property: JsonPropertyName("system")] string System,
        [property: JsonPropertyName("messages")] List<AnthropicMessage> Messages);

    private record AnthropicMessage(
        [property: JsonPropertyName("role")] string Role,
        [property: JsonPropertyName("content")] string Content);

    private record AnthropicResponse([property: JsonPropertyName("content")] List<AnthropicContentBlock> Content);

    private record AnthropicContentBlock(
        [property: JsonPropertyName("type")] string Type,
        [property: JsonPropertyName("text")] string? Text);
}
