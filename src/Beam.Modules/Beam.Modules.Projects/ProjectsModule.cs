using Beam.Modules.Projects.Abstractions;
using Beam.Modules.Projects.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Beam.Modules.Projects;

public static class ProjectsModule
{
    public static IServiceCollection AddProjectsModule(this IServiceCollection services)
    {
        services.AddScoped<IProjectService, ProjectService>();
        services.AddScoped<IProjectAccessService>(sp => (IProjectAccessService)sp.GetRequiredService<IProjectService>());
        return services;
    }
}
