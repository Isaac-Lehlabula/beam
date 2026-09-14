using Beam.Infrastructure;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Beam.Api.HealthChecks;

/// <summary>Backs GET /api/health/ready -- readiness (can we actually serve requests?), as opposed to /api/health's plain liveness ping.</summary>
public class DatabaseHealthCheck(BeamDbContext db) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default)
    {
        try
        {
            return await db.Database.CanConnectAsync(ct)
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Unhealthy("Cannot connect to the database.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Database health check threw.", ex);
        }
    }
}
