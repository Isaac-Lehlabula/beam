using Beam.Modules.Organizations.Abstractions;
using Beam.Modules.Organizations.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Beam.Modules.Organizations;

public static class OrganizationsModule
{
    public static IServiceCollection AddOrganizationsModule(this IServiceCollection services)
    {
        services.AddScoped<IOrganizationService, OrganizationService>();
        services.AddScoped<IOrganizationAccessService>(sp => (IOrganizationAccessService)sp.GetRequiredService<IOrganizationService>());
        return services;
    }
}
