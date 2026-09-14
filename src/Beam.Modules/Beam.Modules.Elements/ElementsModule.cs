using Beam.Modules.Elements.Abstractions;
using Beam.Modules.Elements.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Beam.Modules.Elements;

public static class ElementsModule
{
    public static IServiceCollection AddElementsModule(this IServiceCollection services)
    {
        services.AddScoped<IElementService, ElementService>();
        return services;
    }
}
