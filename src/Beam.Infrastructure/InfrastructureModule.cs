using Amazon.Runtime;
using Amazon.S3;
using Beam.Infrastructure.Storage;
using Beam.Modules.Assets.Abstractions;
using Beam.Modules.Audit.Abstractions;
using Beam.Modules.Elements.Abstractions;
using Beam.Modules.LiveControl.Abstractions;
using Beam.Modules.Organizations.Abstractions;
using Beam.Modules.Overlays.Abstractions;
using Beam.Modules.Projects.Abstractions;
using Beam.Modules.Scenes.Abstractions;
using Beam.Modules.StreamingDestinations.Abstractions;
using Beam.Modules.Templates.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Beam.Infrastructure;

/// <summary>
/// Composition root for everything Beam.Infrastructure provides: the EF Core DbContext (wired
/// to every module's narrow I*DbContext interface) and object storage. Keeping this here
/// means Beam.Api never needs a direct dependency on Npgsql or the AWS SDK.
/// </summary>
public static class InfrastructureModule
{
    public static IServiceCollection AddBeamInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("BeamDb");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Missing ConnectionStrings:BeamDb configuration.");
        }

        services.AddDbContext<BeamDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<IOrganizationsDbContext>(sp => sp.GetRequiredService<BeamDbContext>());
        services.AddScoped<IProjectsDbContext>(sp => sp.GetRequiredService<BeamDbContext>());
        services.AddScoped<IScenesDbContext>(sp => sp.GetRequiredService<BeamDbContext>());
        services.AddScoped<IElementsDbContext>(sp => sp.GetRequiredService<BeamDbContext>());
        services.AddScoped<IAssetsDbContext>(sp => sp.GetRequiredService<BeamDbContext>());
        services.AddScoped<IOverlaysDbContext>(sp => sp.GetRequiredService<BeamDbContext>());
        services.AddScoped<ITemplatesDbContext>(sp => sp.GetRequiredService<BeamDbContext>());
        services.AddScoped<ILiveControlDbContext>(sp => sp.GetRequiredService<BeamDbContext>());
        services.AddScoped<IStreamingDestinationsDbContext>(sp => sp.GetRequiredService<BeamDbContext>());
        services.AddScoped<IAuditDbContext>(sp => sp.GetRequiredService<BeamDbContext>());

        services.Configure<S3StorageOptions>(configuration.GetSection("Storage"));

        var serviceUrl = configuration["Storage:ServiceUrl"];
        var accessKey = configuration["Storage:AccessKey"];
        var secretKey = configuration["Storage:SecretKey"];
        if (string.IsNullOrWhiteSpace(serviceUrl) || string.IsNullOrWhiteSpace(accessKey) || string.IsNullOrWhiteSpace(secretKey))
        {
            throw new InvalidOperationException("Missing Storage:ServiceUrl/AccessKey/SecretKey configuration.");
        }

        // Optional SigV4 signing region. MinIO accepts any, but hosted S3-compatible stores such as
        // Supabase Storage reject requests not signed for the project's own region.
        var region = configuration["Storage:Region"];
        var s3Config = new AmazonS3Config { ServiceURL = serviceUrl, ForcePathStyle = true };
        if (!string.IsNullOrWhiteSpace(region))
        {
            s3Config.AuthenticationRegion = region;
        }

        services.AddSingleton<IAmazonS3>(_ => new AmazonS3Client(new BasicAWSCredentials(accessKey, secretKey), s3Config));
        services.AddScoped<IFileStorageService, S3FileStorageService>();

        return services;
    }

    /// <summary>
    /// Dev-loop convenience so a fresh clone or `docker compose up` doesn't need a manual
    /// migration/bucket-creation step. Production applies migrations as an explicit deploy step
    /// instead of calling this.
    /// </summary>
    public static async Task EnsureBeamInfrastructureReadyAsync(this IServiceProvider services, IConfiguration configuration)
    {
        using var scope = services.CreateScope();

        await scope.ServiceProvider.GetRequiredService<BeamDbContext>().Database.MigrateAsync();

        var s3 = scope.ServiceProvider.GetRequiredService<IAmazonS3>();
        var bucketName = configuration["Storage:BucketName"]!;
        if (!await Amazon.S3.Util.AmazonS3Util.DoesS3BucketExistV2Async(s3, bucketName))
        {
            await s3.PutBucketAsync(bucketName);
        }
    }
}
