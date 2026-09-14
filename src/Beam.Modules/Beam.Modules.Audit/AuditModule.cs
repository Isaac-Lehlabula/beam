using Beam.Modules.Audit.Abstractions;
using Beam.Modules.Audit.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Beam.Modules.Audit;

public static class AuditModule
{
    public static IServiceCollection AddAuditModule(this IServiceCollection services)
    {
        // Registered once as the concrete type, then exposed under both interfaces it
        // implements so callers needing only one side (write vs. read) still share one instance
        // per request.
        services.AddScoped<AuditLogger>();
        services.AddScoped<IAuditLogger>(sp => sp.GetRequiredService<AuditLogger>());
        services.AddScoped<IAuditQueryService>(sp => sp.GetRequiredService<AuditLogger>());
        return services;
    }
}
