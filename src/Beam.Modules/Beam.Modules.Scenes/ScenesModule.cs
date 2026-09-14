using Beam.Modules.Scenes.Abstractions;
using Beam.Modules.Scenes.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Beam.Modules.Scenes;

public static class ScenesModule
{
    public static IServiceCollection AddScenesModule(this IServiceCollection services)
    {
        services.AddScoped<ISceneService, SceneService>();
        services.AddScoped<ISceneAccessService>(sp => (ISceneAccessService)sp.GetRequiredService<ISceneService>());
        return services;
    }
}
