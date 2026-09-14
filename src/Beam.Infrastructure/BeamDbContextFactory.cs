using Beam.Shared.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Beam.Infrastructure;

/// <summary>Lets `dotnet ef migrations` run without spinning up the full API host/DI container.</summary>
public class BeamDbContextFactory : IDesignTimeDbContextFactory<BeamDbContext>
{
    public BeamDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("BEAM_DB_CONNECTION")
            ?? "Host=localhost;Port=5437;Database=beam;Username=beam;Password=beam_dev_password";

        var options = new DbContextOptionsBuilder<BeamDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new BeamDbContext(options, new NullCurrentUserContext());
    }
}
