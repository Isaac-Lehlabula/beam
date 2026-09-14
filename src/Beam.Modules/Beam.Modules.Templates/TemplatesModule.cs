using Beam.Modules.Templates.Abstractions;
using Beam.Modules.Templates.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Beam.Modules.Templates;

public static class TemplatesModule
{
    public static IServiceCollection AddTemplatesModule(this IServiceCollection services)
    {
        services.AddScoped<ITemplateService, TemplateService>();
        return services;
    }
}
