using Beam.Modules.AiGeneration.Abstractions;
using Beam.Modules.AiGeneration.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Beam.Modules.AiGeneration;

public static class AiGenerationModule
{
    public static IServiceCollection AddAiGenerationModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AiGenerationOptions>(configuration.GetSection("AiGeneration"));
        services.AddHttpClient<IAnthropicClient, AnthropicClient>();
        services.AddScoped<IAiProjectGenerationService, AiProjectGenerationService>();
        return services;
    }
}
