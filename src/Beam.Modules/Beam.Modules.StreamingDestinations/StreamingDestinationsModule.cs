using Beam.Modules.StreamingDestinations.Abstractions;
using Beam.Modules.StreamingDestinations.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Beam.Modules.StreamingDestinations;

public static class StreamingDestinationsModule
{
    public static IServiceCollection AddStreamingDestinationsModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<TwitchIntegrationOptions>(configuration.GetSection("Integrations:Twitch"));
        services.Configure<YouTubeIntegrationOptions>(configuration.GetSection("Integrations:YouTube"));

        services.AddHttpClient<IStreamingPlatformIntegration, TwitchIntegration>();
        services.AddHttpClient<IStreamingPlatformIntegration, YouTubeIntegration>();

        services.AddScoped<IStreamingDestinationService, StreamingDestinationService>();
        return services;
    }
}
