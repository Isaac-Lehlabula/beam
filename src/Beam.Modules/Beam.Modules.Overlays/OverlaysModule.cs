using Beam.Modules.Overlays.Abstractions;
using Beam.Modules.Overlays.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Beam.Modules.Overlays;

public static class OverlaysModule
{
    public static IServiceCollection AddOverlaysModule(this IServiceCollection services)
    {
        services.AddScoped<IOverlayService, OverlayService>();
        return services;
    }
}
