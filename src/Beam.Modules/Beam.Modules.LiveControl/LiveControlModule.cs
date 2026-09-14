using Beam.Modules.LiveControl.Abstractions;
using Beam.Modules.LiveControl.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Beam.Modules.LiveControl;

public static class LiveControlModule
{
    public static IServiceCollection AddLiveControlModule(this IServiceCollection services)
    {
        services.AddScoped<ILiveControlService, LiveControlService>();
        return services;
    }
}
